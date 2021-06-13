#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Server.Chemistry.Components;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Notification;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Interaction;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Kitchen.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class MicrowaveComponent : SharedMicrowaveComponent, IActivate, IInteractUsing, ISolutionChange, ISuicideAct
    {
        [Dependency] private readonly RecipeManager _recipeManager = default!;

        #region YAMLSERIALIZE
        [DataField("cookTime")]
        private uint _cookTimeDefault = 5;
        [DataField("cookTimeMultiplier")]
        private int _cookTimeMultiplier = 1000; //For upgrades and stuff I guess?
        [DataField("failureResult")]
        private string _badRecipeName = "FoodBadRecipe";
        [DataField("beginCookingSound")]
        private string _startCookingSound = "/Audio/Machines/microwave_start_beep.ogg";
        [DataField("foodDoneSound")]
        private string _cookingCompleteSound = "/Audio/Machines/microwave_done_beep.ogg";
#endregion

[ViewVariables]
        private bool _busy = false;

        /// <summary>
        /// This is a fixed offset of 5.
        /// The cook times for all recipes should be divisible by 5,with a minimum of 1 second.
        /// For right now, I don't think any recipe cook time should be greater than 60 seconds.
        /// </summary>
        [ViewVariables]
        private uint _currentCookTimerTime = 1;

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;
        private bool _hasContents => Owner.TryGetComponent(out SolutionContainerComponent? solution) && (solution.ReagentList.Count > 0 || _storage.ContainedEntities.Count > 0);
        private bool _uiDirty = true;
        private bool _lostPower = false;
        private int _currentCookTimeButtonIndex = 0;

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs) => _uiDirty = true;
        private AudioSystem _audioSystem = default!;
        private Container _storage = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(MicrowaveUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            _currentCookTimerTime = _cookTimeDefault;

            Owner.EnsureComponent<SolutionContainerComponent>();

            _storage = ContainerHelpers.EnsureContainer<Container>(Owner, "microwave_entity_container", out var existed);
            _audioSystem = EntitySystem.Get<AudioSystem>();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            if (!Powered || _busy)
            {
                return;
            }

            switch (message.Message)
            {
                case MicrowaveStartCookMessage msg :
                    Wzhzhzh();
                    break;
                case MicrowaveEjectMessage msg :
                    if (_hasContents)
                    {
                        VaporizeReagents();
                        EjectSolids();
                        ClickSound();
                        _uiDirty = true;
                    }
                    break;
                case MicrowaveEjectSolidIndexedMessage msg:
                    if (_hasContents)
                    {
                        EjectSolid(msg.EntityID);
                        ClickSound();
                        _uiDirty = true;
                    }
                    break;
                case MicrowaveVaporizeReagentIndexedMessage msg:
                    if (_hasContents)
                    {
                        VaporizeReagentQuantity(msg.ReagentQuantity);
                        ClickSound();
                        _uiDirty = true;
                    }
                    break;
                case MicrowaveSelectCookTimeMessage msg:
                    _currentCookTimeButtonIndex = msg.ButtonIndex;
                    _currentCookTimerTime = msg.NewCookTime;
                    ClickSound();
                    _uiDirty = true;
                    break;
            }

        }

        public void OnUpdate()
        {

            if (!Powered)
            {
                //TODO:If someone cuts power currently, microwave magically keeps going. FIX IT!
                SetAppearance(MicrowaveVisualState.Idle);
            }

            if (_busy && !Powered)
            {
                //we lost power while we were cooking/busy!
                _lostPower = true;
                VaporizeReagents();
                EjectSolids();
                _busy = false;
                _uiDirty = true;
            }

            if (_uiDirty && Owner.TryGetComponent(out SolutionContainerComponent? solution))
            {
                UserInterface?.SetState(new MicrowaveUpdateUserInterfaceState
                (
                    solution.Solution.Contents.ToArray(),
                    _storage.ContainedEntities.Select(item => item.Uid).ToArray(),
                    _busy,
                    _currentCookTimeButtonIndex,
                    _currentCookTimerTime
                ));
                _uiDirty = false;
            }
        }

        private void SetAppearance(MicrowaveVisualState state)
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(PowerDeviceVisuals.VisualState, state);
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out ActorComponent? actor) || !Powered)
            {
                return;
            }

            _uiDirty = true;
            UserInterface?.Toggle(actor.PlayerSession);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!Powered)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("It has no power!"));
                return false;
            }

            var itemEntity = eventArgs.User.GetComponent<HandsComponent>().GetActiveHand?.Owner;

            if (itemEntity == null)
            {
                eventArgs.User.PopupMessage(Loc.GetString("You have no active hand!"));
                return false;
            }

            if (itemEntity.TryGetComponent<SolutionTransferComponent>(out var attackPourable))
            {
                if (!itemEntity.TryGetComponent<ISolutionInteractionsComponent>(out var attackSolution)
                    || !attackSolution.CanDrain)
                {
                    return false;
                }

                if (!Owner.TryGetComponent(out SolutionContainerComponent? solution))
                {
                    return false;
                }

                //Get transfer amount. May be smaller than _transferAmount if not enough room
                var realTransferAmount = ReagentUnit.Min(attackPourable.TransferAmount, solution.EmptyVolume);
                if (realTransferAmount <= 0) //Special message if container is full
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Container is full"));
                    return false;
                }

                //Move units from attackSolution to targetSolution
                var removedSolution = attackSolution.Drain(realTransferAmount);
                if (!solution.TryAddSolution(removedSolution))
                {
                    return false;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("Transferred {0}u", removedSolution.TotalVolume));
                return true;
            }

            if (!itemEntity.TryGetComponent(typeof(ItemComponent), out var food))
            {

                Owner.PopupMessage(eventArgs.User, "That won't work!");
                return false;
            }

            var ent = food.Owner; //Get the entity of the ItemComponent.
            _storage.Insert(ent);
            _uiDirty = true;
            return true;
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private void Wzhzhzh()
        {
            if (!_hasContents)
            {
                return;
            }

            _busy = true;
            // Convert storage into Dictionary of ingredients
            var solidsDict = new Dictionary<string, int>();
            foreach(var item in _storage.ContainedEntities)
            {
                if (item.Prototype == null)
                {
                    continue;
                }

                if(solidsDict.ContainsKey(item.Prototype.ID))
                {
                    solidsDict[item.Prototype.ID]++;
                }
                else
                {
                    solidsDict.Add(item.Prototype.ID, 1);
                }
            }

            var failState = MicrowaveSuccessState.RecipeFail;
            foreach(var id in solidsDict.Keys)
            {
                if(_recipeManager.SolidAppears(id))
                {
                    continue;
                }

                failState = MicrowaveSuccessState.UnwantedForeignObject;
                break;
            }

            // Check recipes
            FoodRecipePrototype? recipeToCook = null;
            foreach (var r in _recipeManager.Recipes.Where(r => CanSatisfyRecipe(r, solidsDict) == MicrowaveSuccessState.RecipePass))
            {
                recipeToCook = r;
            }

            SetAppearance(MicrowaveVisualState.Cooking);
            SoundSystem.Play(Filter.Pvs(Owner), _startCookingSound, Owner, AudioParams.Default);
            Owner.SpawnTimer((int)(_currentCookTimerTime * _cookTimeMultiplier), (Action)(() =>
            {
                if (_lostPower)
                {
                    return;
                }

                if(failState == MicrowaveSuccessState.UnwantedForeignObject)
                {
                    VaporizeReagents();
                    EjectSolids();
                }
                else
                {
                    if (recipeToCook != null)
                    {
                        SubtractContents(recipeToCook);
                        Owner.EntityManager.SpawnEntity(recipeToCook.Result, Owner.Transform.Coordinates);
                    }
                    else
                    {
                        VaporizeReagents();
                        VaporizeSolids();
                        Owner.EntityManager.SpawnEntity(_badRecipeName, Owner.Transform.Coordinates);
                    }
                }
                SoundSystem.Play(Filter.Pvs(Owner), _cookingCompleteSound, Owner, AudioParams.Default.WithVolume(-1f));

                SetAppearance(MicrowaveVisualState.Idle);
                _busy = false;

                _uiDirty = true;
            }));
            _lostPower = false;
            _uiDirty = true;
        }

        private void VaporizeReagents()
        {
            if (Owner.TryGetComponent(out SolutionContainerComponent? solution))
            {
                solution.RemoveAllSolution();
            }
        }

        private void VaporizeReagentQuantity(Solution.ReagentQuantity reagentQuantity)
        {
            if (Owner.TryGetComponent(out SolutionContainerComponent? solution))
            {
                solution?.TryRemoveReagent(reagentQuantity.ReagentId, reagentQuantity.Quantity);
            }
        }

        private void VaporizeSolids()
        {
            for(var i = _storage.ContainedEntities.Count-1; i>=0; i--)
            {
                var item = _storage.ContainedEntities.ElementAt(i);
                _storage.Remove(item);
                item.Delete();
            }
        }

        private void EjectSolids()
        {

            for(var i = _storage.ContainedEntities.Count-1; i>=0; i--)
            {
                _storage.Remove(_storage.ContainedEntities.ElementAt(i));
            }
        }

        private void EjectSolid(EntityUid entityID)
        {
            if (Owner.EntityManager.EntityExists(entityID))
            {
                _storage.Remove(Owner.EntityManager.GetEntity(entityID));
            }
        }


        private void SubtractContents(FoodRecipePrototype recipe)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent? solution))
            {
                return;
            }

            foreach(var recipeReagent in recipe.IngredientsReagents)
            {
                solution?.TryRemoveReagent(recipeReagent.Key, ReagentUnit.New(recipeReagent.Value));
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                for (var i = 0; i < recipeSolid.Value; i++)
                {
                    foreach (var item in _storage.ContainedEntities)
                    {
                        if (item.Prototype == null)
                        {
                            continue;
                        }

                        if (item.Prototype.ID == recipeSolid.Key)
                        {
                            _storage.Remove(item);
                            item.Delete();
                            break;
                        }
                    }
                }
            }

        }

        private MicrowaveSuccessState CanSatisfyRecipe(FoodRecipePrototype recipe, Dictionary<string,int> solids)
        {
            if (_currentCookTimerTime != (uint) recipe.CookTime)
            {
                return MicrowaveSuccessState.RecipeFail;
            }

            if (!Owner.TryGetComponent(out SolutionContainerComponent? solution))
            {
                return MicrowaveSuccessState.RecipeFail;
            }

            foreach (var reagent in recipe.IngredientsReagents)
            {
                if (!solution.Solution.ContainsReagent(reagent.Key, out var amount))
                {
                    return MicrowaveSuccessState.RecipeFail;
                }

                if (amount.Int() < reagent.Value)
                {
                    return MicrowaveSuccessState.RecipeFail;
                }
            }

            foreach (var solid in recipe.IngredientsSolids)
            {
                if (!solids.ContainsKey(solid.Key))
                {
                    return MicrowaveSuccessState.RecipeFail;
                }

                if (solids[solid.Key] < solid.Value)
                {
                    return MicrowaveSuccessState.RecipeFail;
                }
            }

            return MicrowaveSuccessState.RecipePass;
        }

        private void ClickSound()
        {
            SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Machines/machine_switch.ogg",Owner,AudioParams.Default.WithVolume(-2f));
        }

        SuicideKind ISuicideAct.Suicide(IEntity victim, IChatManager chat)
        {
            var headCount = 0;

            if (victim.TryGetComponent<IBody>(out var body))
            {
                var headSlots = body.GetSlotsOfType(BodyPartType.Head);

                foreach (var slot in headSlots)
                {
                    var part = slot.Part;

                    if (part == null ||
                        !body.TryDropPart(slot, out var dropped))
                    {
                        continue;
                    }

                    foreach (var droppedPart in dropped.Values)
                    {
                        if (droppedPart.PartType != BodyPartType.Head)
                        {
                            continue;
                        }

                        _storage.Insert(droppedPart.Owner);
                        headCount++;
                    }
                }
            }

            var othersMessage = headCount > 1
                ? Loc.GetString("{0:theName} is trying to cook {0:their} heads!", victim)
                : Loc.GetString("{0:theName} is trying to cook {0:their} head!", victim);

            victim.PopupMessageOtherClients(othersMessage);

            var selfMessage = headCount > 1
                ? Loc.GetString("You cook your heads!")
                : Loc.GetString("You cook your head!");

            victim.PopupMessage(selfMessage);

            _currentCookTimerTime = 10;
            ClickSound();
            _uiDirty = true;
            Wzhzhzh();
            return SuicideKind.Heat;
        }
    }
}
