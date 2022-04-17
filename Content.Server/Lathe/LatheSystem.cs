using Content.Server.Lathe.Components;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Components;
using Content.Shared.Interaction;
using Content.Server.Materials;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using JetBrains.Annotations;

namespace Content.Server.Lathe
{
    [UsedImplicitly]
    public sealed class LatheSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LatheComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<LatheComponent, ComponentInit>(OnComponentInit);
        }

        // These queues are to add/remove COMPONENTS to the lathes
        private Queue<EntityUid> ProducingAddQueue = new();
        private Queue<EntityUid> ProducingRemoveQueue = new();
        private Queue<EntityUid> InsertingAddQueue = new();
        private Queue<EntityUid> InsertingRemoveQueue = new();

        public override void Update(float frameTime)
        {
            foreach (var uid in ProducingAddQueue)
                EnsureComp<LatheProducingComponent>(uid);
            ProducingAddQueue.Clear();
            foreach (var uid in ProducingRemoveQueue)
                RemComp<LatheProducingComponent>(uid);
            ProducingRemoveQueue.Clear();
            foreach (var uid in InsertingAddQueue)
                EnsureComp<LatheInsertingComponent>(uid);
            InsertingAddQueue.Clear();
            foreach (var uid in InsertingRemoveQueue)
                RemComp<LatheInsertingComponent>(uid);
            InsertingRemoveQueue.Clear();

            foreach (var (insertingComp, lathe) in EntityQuery<LatheInsertingComponent, LatheComponent>(false))
            {
                if (lathe.InsertionAccumulator < lathe.InsertionTime)
                {
                    lathe.InsertionAccumulator += frameTime;
                    continue;
                }
                lathe.InsertionAccumulator = 0;
                UpdateInsertingAppearance(lathe.Owner, false);
                InsertingRemoveQueue.Enqueue(lathe.Owner);
            }

            foreach (var (producingComp, lathe) in EntityQuery<LatheProducingComponent, LatheComponent>(false))
            {
                if (lathe.ProducingRecipe == null)
                    continue;
                if (lathe.ProducingAccumulator < lathe.ProducingRecipe.CompleteTime.TotalSeconds)
                {
                    lathe.ProducingAccumulator += frameTime;
                    continue;
                }
                lathe.ProducingAccumulator = 0;

                FinishProducing(lathe.ProducingRecipe, lathe);
            }
        }

        /// <summary>
        /// Initialize the UI and appearance.
        /// Appearance requires initialization or the layers break
        /// </summary>
        private void OnComponentInit(EntityUid uid, LatheComponent component, ComponentInit args)
        {
            component.UserInterface = uid.GetUIOrNull(LatheUiKey.Key);
            if (component.UserInterface != null)
            {
                component.UserInterface.OnReceiveMessage += msg => UserInterfaceOnOnReceiveMessage(uid, component, msg);
            }

            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;
            appearance.SetData(LatheVisuals.IsInserting, false);
            appearance.SetData(LatheVisuals.IsRunning, false);
        }

        /// <summary>
        /// When someone tries to use an item on the lathe,
        /// insert it if it's a stack and fits inside
        /// </summary>
        private void OnInteractUsing(EntityUid uid, LatheComponent component, InteractUsingEvent args)
        {
            if (!TryComp<MaterialStorageComponent>(uid, out var storage) || !TryComp<MaterialComponent>(args.Used, out var material))
                return;

            var multiplier = 1;

            if (TryComp<StackComponent>(args.Used, out var stack))
                multiplier = stack.Count;

            var totalAmount = 0;

            // Check if it can insert all materials.
            foreach (var mat in material.MaterialIds)
            {
                // TODO: Change how MaterialComponent works so this is not hard-coded.
                if (!storage.CanInsertMaterial(mat, component.VolumePerSheet * multiplier))
                    return;
                totalAmount += component.VolumePerSheet * multiplier;
            }

            // Check if it can take ALL of the material's volume.
            if (storage.StorageLimit > 0 && !storage.CanTakeAmount(totalAmount))
                return;
            var lastMat = string.Empty;
            foreach (var mat in material.MaterialIds)
            {
                storage.InsertMaterial(mat, component.VolumePerSheet * multiplier);
                lastMat = mat;
            }
            /// We need the prototype to get the color
            _prototypeManager.TryIndex(lastMat, out MaterialPrototype? matProto);

            EntityManager.QueueDeleteEntity(args.Used);
            InsertingAddQueue.Enqueue(uid);
            _popupSystem.PopupEntity(Loc.GetString("machine-insert-item", ("machine", uid),
                ("item", args.Used)), uid, Filter.Entities(args.User));
            if (matProto != null)
            {
                UpdateInsertingAppearance(uid, true, matProto.Color);
            }
            UpdateInsertingAppearance(uid, true);
        }

        /// <summary>
        /// This handles the checks to start producing an item, and
        /// starts up the sound and visuals
        /// </summary>
        private bool Produce(LatheComponent component, LatheRecipePrototype recipe, bool SkipCheck = false)
        {
            if (!component.CanProduce(recipe)
                || !TryComp(component.Owner, out MaterialStorageComponent? storage))
                return false;

            if (!SkipCheck && HasComp<LatheProducingComponent>(component.Owner))
                return false;

            if (TryComp<ApcPowerReceiverComponent>(component.Owner, out var receiver) && !receiver.Powered)
                return false;

            component.UserInterface?.SendMessage(new LatheFullQueueMessage(GetIdQueue(component)));

            component.ProducingRecipe = recipe;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                // This should always return true, otherwise CanProduce fucked up.
                storage.RemoveMaterial(material, amount);
            }

            component.UserInterface?.SendMessage(new LatheProducingRecipeMessage(recipe.ID));
            if (component.ProducingSound != null)
            {
                SoundSystem.Play(Filter.Pvs(component.Owner), component.ProducingSound.GetSound(), component.Owner);
            }
            UpdateRunningAppearance(component.Owner, true);
            ProducingAddQueue.Enqueue(component.Owner);
            return true;
        }

        /// <summary>
        /// After the production timer is up, this spawns the recipe and
        /// either cleans up or continues to the next item in the queue
        /// </summary>
        private void FinishProducing(LatheRecipePrototype recipe, LatheComponent component)
        {
            component.ProducingRecipe = null;
            EntityManager.SpawnEntity(recipe.Result, Comp<TransformComponent>(component.Owner).Coordinates);
            component.UserInterface?.SendMessage(new LatheStoppedProducingRecipeMessage());
            // Continue to next in queue if there are items left
            if (component.Queue.Count > 0)
            {
                Produce(component, component.Queue.Dequeue(), true);
                return;
            }
            ProducingRemoveQueue.Enqueue(component.Owner);
            UpdateRunningAppearance(component.Owner, false);
        }

        /// <summary>
        /// Sets the machine sprite to either play the running animation
        /// or stop.
        /// </summary>
        private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(LatheVisuals.IsRunning, isRunning);
        }

        /// <summary>
        /// Sets the machine sprite to play the inserting animation
        /// and sets the color of the inserted mat if applicable
        /// </summary>
        private void UpdateInsertingAppearance(EntityUid uid, bool isInserting, Color? color = null)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(LatheVisuals.IsInserting, isInserting);
            if (color != null)
                appearance.SetData(LatheVisuals.InsertingColor, color);
        }

        /// <summary>
        /// Handles all the button presses in the lathe UI
        /// </summary>
        private void UserInterfaceOnOnReceiveMessage(EntityUid uid, LatheComponent component, ServerBoundUserInterfaceMessage message)
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver) && !receiver.Powered)
                return;

            switch (message.Message)
            {
                case LatheQueueRecipeMessage msg:
                    _prototypeManager.TryIndex(msg.ID, out LatheRecipePrototype? recipe);
                    if (recipe != null!)
                        for (var i = 0; i < msg.Quantity; i++)
                        {
                            component.Queue.Enqueue(recipe);
                            component.UserInterface?.SendMessage(new LatheFullQueueMessage(GetIdQueue(component)));
                        }
                        if (!HasComp<LatheProducingComponent>(component.Owner) && component.Queue.Count > 0)
                            Produce(component, component.Queue.Dequeue());

                    break;
                case LatheSyncRequestMessage _:
                    if (!HasComp<MaterialStorageComponent>(uid)) return;
                    component.UserInterface?.SendMessage(new LatheFullQueueMessage(GetIdQueue(component)));
                    if (component.ProducingRecipe != null)
                        component.UserInterface?.SendMessage(new LatheProducingRecipeMessage(component.ProducingRecipe.ID));
                    break;

                case LatheServerSelectionMessage _:
                    if (!TryComp(uid, out ResearchClientComponent? researchClient)) return;
                    researchClient.OpenUserInterface(message.Session);
                    break;

                case LatheServerSyncMessage _:
                    if (!TryComp(uid, out TechnologyDatabaseComponent? database)
                    || !TryComp(uid, out ProtolatheDatabaseComponent? protoDatabase)) return;

                    if (database.SyncWithServer())
                        protoDatabase.Sync();

                    break;
            }
        }

        /// <summary>
        /// Gets all the prototypes in the lathe's construction queue
        /// </summary>
        private Queue<string> GetIdQueue(LatheComponent lathe)
        {
            var queue = new Queue<string>();
            foreach (var recipePrototype in lathe.Queue)
            {
                queue.Enqueue(recipePrototype.ID);
            }
            return queue;
        }
    }
}
