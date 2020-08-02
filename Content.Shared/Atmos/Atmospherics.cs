﻿using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos
{
    /// <summary>
    ///     Class to store atmos constants.
    /// </summary>
    public static class Atmospherics
    {
        static Atmospherics()
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();

            GasPrototypes = new GasPrototype[TotalNumberOfGases];

            for (var i = 0; i < TotalNumberOfGases; i++)
            {
                GasPrototypes[i] = protoMan.Index<GasPrototype>(i.ToString());
            }
        }

        private static readonly GasPrototype[] GasPrototypes;

        public static GasPrototype GetGas(int gasId) => GasPrototypes[gasId];
        public static GasPrototype GetGas(Gas gasId) => GasPrototypes[(int) gasId];
        public static IEnumerable<GasPrototype> Gases => GasPrototypes;

        #region ATMOS
        /// <summary>
        ///     The universal gas constant, in kPa*L/(K*mol)
        /// </summary>
        public const float R = 8.314462618f;

        /// <summary>
        ///     1 ATM in kPA.
        /// </summary>
        public const float OneAtmosphere = 101.325f;

        /// <summary>
        ///     -270.3ºC in K. CMB stands for Cosmic Microwave Background.
        /// </summary>
        public const float TCMB = 2.7f;

        /// <summary>
        ///     0ºC in K
        /// </summary>
        public const float T0C = 273.15f;

        /// <summary>
        ///     20ºC in K
        /// </summary>
        public const float T20C = 293.15f;

        /// <summary>
        ///     Liters in a cell.
        /// </summary>
        public const float CellVolume = 2500f;

        /// <summary>
        ///     Moles in a 2.5 m^3 cell at 101.325 Pa and 20ºC
        /// </summary>
        public const float MolesCellStandard = (OneAtmosphere * CellVolume / (T20C * R));

        #endregion

        /// <summary>
        ///     Visible moles multiplied by this factor to get moles at which gas is at max visibility.
        /// </summary>
        public const float FactorGasVisibleMax = 20f;

        /// <summary>
        ///     Minimum number of moles a gas can have.
        /// </summary>
        public const float GasMinMoles = 0.00000005f;

        public const float OpenHeatTransferCoefficient = 0.4f;

        /// <summary>
        ///     Ratio of air that must move to/from a tile to reset group processing
        /// </summary>
        public const float MinimumAirRatioToSuspend = 0.1f;

        /// <summary>
        ///     Minimum ratio of air that must move to/from a tile
        /// </summary>
        public const float MinimumAirRatioToMove = 0.001f;

        /// <summary>
        ///     Minimum amount of air that has to move before a group processing can be suspended
        /// </summary>
        public const float MinimumAirToSuspend = (MolesCellStandard * MinimumAirRatioToSuspend);

        public const float MinimumTemperatureToMove = (T20C + 100f);

        public const float MinimumMolesDeltaToMove = (MolesCellStandard * MinimumAirRatioToMove);

        /// <summary>
        ///     Minimum temperature difference before group processing is suspended
        /// </summary>
        public const float MinimumTemperatureDeltaToSuspend = 4.0f;

        /// <summary>
        ///     Minimum temperature difference before the gas temperatures are just set to be equal.
        /// </summary>
        public const float MinimumTemperatureDeltaToConsider = 0.5f;

        /// <summary>
        ///     Minimum temperature for starting superconduction.
        /// </summary>
        public const float MinimumTemperatureStartSuperConduction = (T20C + 200f);

        /// <summary>
        ///     Minimum heat capacity.
        /// </summary>
        public const float MinimumHeatCapacity = 0.0003f;

        #region Excited Groups

        /// <summary>
        ///     Number of full atmos updates ticks before an excited group breaks down (averages gas contents across turfs)
        /// </summary>
        public const int ExcitedGroupBreakdownCycles = 4;

        /// <summary>
        ///     Number of full atmos updates before an excited group dismantles and removes its turfs from active
        /// </summary>
        public const int ExcitedGroupsDismantleCycles = 16;

        #endregion

        /// <summary>
        ///     Hard limit for tile equalization.
        /// </summary>
        public const int ZumosHardTileLimit = 2000;

        /// <summary>
        ///     Limit for zone-based tile equalization.
        /// </summary>
        public const int ZumosTileLimit = 200;

        /// <summary>
        ///     Total number of gases. Increase this if you want to add more!
        /// </summary>
        public const int TotalNumberOfGases = 5;

        public const float FireMinimumTemperatureToExist = T0C + 100f;
        public const float FireMinimumTemperatureToSpread = T0C + 150f;
        public const float FireSpreadRadiosityScale = 0.85f;
        public const float FirePhoronEnergyReleased = 3000000f;
        public const float FireGrowthRate = 40000f;

        public const float SuperSaturationThreshold = 96f;

        public const float OxygenBurnRateBase = 1.4f;
        public const float PhoronMinimumBurnTemperature = (100f+T0C);
        public const float PhoronUpperTemperature = (1370f+T0C);
        public const float PhoronOxygenFullburn = 10f;
        public const float PhoronBurnRateDelta = 9f;
    }

    /// <summary>
    ///     Gases to Ids. Keep these updated with the prototypes!
    /// </summary>
    public enum Gas
    {
        Oxygen = 0,
        Nitrogen = 1,
        CarbonDioxide = 2,
        Phoron = 3,
        Tritium = 4,
    }
}
