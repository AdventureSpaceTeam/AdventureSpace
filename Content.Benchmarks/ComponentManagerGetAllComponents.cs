﻿using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Moq;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Benchmarks
{
    public class ComponentManagerGetAllComponents
    {
        private readonly List<IEntity> _entities = new();

        private IComponentManager _componentManager;

        [Params(5000)] public int N { get; set; }

        public static void TestRun()
        {
            var x = new ComponentManagerGetAllComponents
            {
                N = 500
            };
            x.Setup();
            x.Run();
        }

        [GlobalSetup]
        public void Setup()
        {
            // Initialize component manager.
            IoCManager.InitThread();

            IoCManager.Register<IComponentManager, ComponentManager>();
            IoCManager.Register<IRuntimeLog, RuntimeLog>();
            IoCManager.Register<ILogManager, LogManager>();
            IoCManager.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            IoCManager.Register<IEntitySystemManager, EntitySystemManager>();
            var entityManager = new Mock<IEntityManager>().Object;
            IoCManager.RegisterInstance<IEntityManager>(entityManager);
            IoCManager.RegisterInstance<IReflectionManager>(new Mock<IReflectionManager>().Object);

            var dummyReg = new Mock<IComponentRegistration>();
            dummyReg.SetupGet(p => p.Name).Returns("Dummy");
            dummyReg.SetupGet(p => p.Type).Returns(typeof(DummyComponent));
            dummyReg.SetupGet(p => p.NetID).Returns((uint?) null);
            dummyReg.SetupGet(p => p.NetworkSynchronizeExistence).Returns(false);
            dummyReg.SetupGet(p => p.References).Returns(new [] {typeof(DummyComponent)});

            var componentFactory = new Mock<IComponentFactory>();
            componentFactory.Setup(p => p.GetComponent<DummyComponent>()).Returns(new DummyComponent());
            componentFactory.Setup(p => p.GetRegistration(It.IsAny<DummyComponent>())).Returns(dummyReg.Object);
            componentFactory.Setup(p => p.GetAllRefTypes()).Returns(new[] {typeof(DummyComponent)});

            IoCManager.RegisterInstance<IComponentFactory>(componentFactory.Object);

            IoCManager.BuildGraph();
            _componentManager = IoCManager.Resolve<IComponentManager>();
            _componentManager.Initialize();

            // Initialize N entities with one component.
            for (var i = 0; i < N; i++)
            {
                var entity = new Entity();
                entity.SetManagers(entityManager);
                entity.SetUid(new EntityUid(i + 1));
                _entities.Add(entity);

                _componentManager.AddComponent<DummyComponent>(entity);
            }
        }

        [Benchmark]
        public int Run()
        {
            var count = 0;

            foreach (var _ in _componentManager.EntityQuery<DummyComponent>(true))
            {
                count += 1;
            }

            return count;
        }

        [Benchmark]
        public int Noop()
        {
            var count = 0;

            _componentManager.TryGetComponent(default, out DummyComponent _);

            return count;
        }

        private class DummyComponent : Component
        {
            public override string Name => "Dummy";
        }
    }
}
