﻿using System;
using System.Collections.Generic;
using Content.Shared.Prototypes.Kitchen;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen
{
    
    public class RecipeManager
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649
        public List<FoodRecipePrototype> Recipes { get; private set; }

        public void Initialize()
        {
            Recipes = new List<FoodRecipePrototype>();
            foreach (var item in _prototypeManager.EnumeratePrototypes<FoodRecipePrototype>())
            {
                Recipes.Add(item);
            }

            Recipes.Sort(new RecipeComparer());
        }
        private class RecipeComparer : Comparer<FoodRecipePrototype>
        {
            public override int Compare(FoodRecipePrototype x, FoodRecipePrototype y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }
                
                return -x.IngredientsReagents.Count.CompareTo(y.IngredientsReagents.Count);
            }
        }
    }
}
