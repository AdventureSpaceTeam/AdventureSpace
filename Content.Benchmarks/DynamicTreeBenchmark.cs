﻿using BenchmarkDotNet.Attributes;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Benchmarks
{
    [SimpleJob, MemoryDiagnoser]
    public class DynamicTreeBenchmark
    {
        private static readonly Box2[] aabbs1 =
        {
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            new Box2(-3, 3, -3, 3), // point off to the bottom left
            new Box2(-3, -3, -3, -3), // point off to the top left
            new Box2(3, 3, 3, 3), // point off to the bottom right
            new Box2(3, -3, 3, -3), // point off to the top right
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            ((Box2) default).Enlarged(3), //6x6 square
            new Box2(-3, 3, -3, 3), // point off to the bottom left
            new Box2(-3, -3, -3, -3), // point off to the top left
            new Box2(3, 3, 3, 3), // point off to the bottom right
            new Box2(3, -3, 3, -3), // point off to the top right
        };

        private B2DynamicTree<int> _b2Tree;
        private DynamicTree<int> _tree;

        [GlobalSetup]
        public void Setup()
        {
            _b2Tree = new B2DynamicTree<int>();
            _tree = new DynamicTree<int>((in int value) => aabbs1[value], capacity: 16);

            for (var i = 0; i < aabbs1.Length; i++)
            {
                var aabb = aabbs1[i];
                _b2Tree.CreateProxy(aabb, i);
                _tree.Add(i);
            }
        }

        [Benchmark]
        public void BenchB2()
        {
            object state = null;
            _b2Tree.Query(ref state, (ref object _, DynamicTree.Proxy __) => true, new Box2(-1, -1, 1, 1));
        }

        [Benchmark]
        public void BenchQ()
        {
            foreach (var _ in _tree.QueryAabb(new Box2(-1, -1, 1, 1), true))
            {

            }
        }
    }
}
