using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	// 1.8.43: one DLL for both builds. PerformanceProfile picks stock or buffed performance at runtime.
	public enum PerformanceProfile
	{
		Buffed,
		Stock
	}

	internal static class Plugin
	{
		internal static ManualLogSource Log;

		private static ConfigEntry<bool> cEnabled;

		private static ConfigEntry<PerformanceProfile> cProfile;

		private static ConfigEntry<bool> cTelemetry;

		private static ConfigEntry<bool> cAIFcs;

		private static ConfigEntry<bool> cAILog;

		private static ConfigEntry<bool> cAIDirectRoll;

		private static ConfigEntry<float> cStabUp;

		private static ConfigEntry<float> cStabDn;

		private static ConfigEntry<float> cRudRange;

		private static ConfigEntry<float> cFlapRange;

		private static ConfigEntry<float> cTvcRange;

		private static ConfigEntry<float> cTvcSpeed;

		private static ConfigEntry<bool> cAIGunRollFade;

		private static ConfigEntry<bool> cAsymOn;

		private static ConfigEntry<bool> cAsymRollHead;

		private static ConfigEntry<bool> cRevRoll;

		private static ConfigEntry<bool> cGndBrakeLog;


		private static ConfigEntry<float> cAoAPos;

		private static ConfigEntry<float> cAoANeg;

		private static ConfigEntry<float> cGPos;

		private static ConfigEntry<float> cGNeg;

		private static ConfigEntry<float> cMpoGPos;

		private static ConfigEntry<float> cMpoGNeg;

		private static ConfigEntry<float> cMaxPitch;

		private static ConfigEntry<float> cProt;

		private static ConfigEntry<float> cMaxRoll;

		private static ConfigEntry<float> cMaxYaw;

		private static ConfigEntry<float> cCouple;

		private static ConfigEntry<float> cPedalBeta;

		private static ConfigEntry<float> cKBeta;

		private static ConfigEntry<float> cKRoll;

		private static ConfigEntry<float> cKPitch;

		private static ConfigEntry<float> cKYaw;

		private static ConfigEntry<float> cKpAoA;

		private static ConfigEntry<bool> cPairTrim;

		private static ConfigEntry<float> cLatRes;

		private static ConfigEntry<bool> cBranch;

		private static ConfigEntry<bool> cOvPrio;

		private static ConfigEntry<float> cCoupRoll;

		private static ConfigEntry<float> cBranchMaxAoA;

		private static ConfigEntry<float> cBranchTarget;

		private static ConfigEntry<float> cBranchTol;

		private static ConfigEntry<float> cAntiWind;

		private static ConfigEntry<bool> cThrBrake;

		private static ConfigEntry<float> cBrakeBlend;

		private static ConfigEntry<float> cKiAoA;

		private static ConfigEntry<float> cKpG;

		private static ConfigEntry<float> cKiG;

		private static ConfigEntry<float> cFlaperonEff;

		private static ConfigEntry<float> cStabEff;

		private static ConfigEntry<float> cActuator;

		private static ConfigEntry<float> cRudStart;

		private static ConfigEntry<float> cRudEnd;

		private static ConfigEntry<float> cUnloadDecel;

		private static ConfigEntry<float> cUnloadRise;

		private static ConfigEntry<float> cRollLo;

		private static ConfigEntry<float> cRollHi;

		private static ConfigEntry<float> cRollAuth;

		private static ConfigEntry<float> cRollBrake;

		private static ConfigEntry<float> cStabTrim;

		private static ConfigEntry<float> cLerxEff;

		private static ConfigEntry<float> cCgShift;

		private static ConfigEntry<float> cCoupFF;

		private static ConfigEntry<float> cRelPitchPrio;

		private static ConfigEntry<float> cRelStabRate;

		private static ConfigEntry<float> cWashSlew;

		private static ConfigEntry<float> cMomLead;

		private static ConfigEntry<float> cOnset;

		private static ConfigEntry<float> cStabRoll;

		private static ConfigEntry<float> cStabRollShare;

		private static ConfigEntry<float> cStickSmooth;

		private static ConfigEntry<float> cActDamp;

		private static ConfigEntry<float> cYawStop;

		private static ConfigEntry<float> cOuterFlap;

		private static ConfigEntry<float> cRollAcc;

		private static ConfigEntry<float> cThrust;

		private static ConfigEntry<float> cDrag;

		private static ConfigEntry<float> cDrAoA;



		private static ConfigEntry<float> cDrTrans;


		private static ConfigEntry<float> cYawPri;

		private static ConfigEntry<float> cYawFF;

		private static ConfigEntry<float> cRollLead;

		private static ConfigEntry<float> cFlareScaleStock;

		private static ConfigEntry<float> cFlareScaleBuffed;

		private static ConfigEntry<float> cEwCapStock;

		private static ConfigEntry<float> cEwCapBuffed;

		private static ConfigEntry<float> cEwRateStock;

		private static ConfigEntry<float> cEwRateBuffed;

		private static ConfigEntry<float> cLowYaw;

		private static ConfigEntry<float> cLowYawIas;

		private static ConfigEntry<float> cLowYawFall;

		private static ConfigEntry<float> cTvcMove;

		private static ConfigEntry<float> cRollTvc;

		private static ConfigEntry<float> cRollWash;

		private static ConfigEntry<float> cRollTvcBudget;

		private static ConfigEntry<float> cRollTvcBudgetCost;

		private static ConfigEntry<float> cRudSideCap;

		private static ConfigEntry<bool> cNwsFbw;

		private static ConfigEntry<float> cGndYawRate;

		private static ConfigEntry<float> cGndLatAcc;

		private static ConfigEntry<float> cGndLine;

		private static ConfigEntry<bool> cNozAnimAI;

		private static ConfigEntry<bool> cNozAnim;

		private static ConfigEntry<float> cAsymPitchMargin;

		private static ConfigEntry<bool> cAsymRollCoupling;

		private static ConfigEntry<bool> cRollPitchPriority;

		private static ConfigEntry<float> cRollFightCost;

		private static ConfigEntry<float> cNozSeamShade;

		private static ConfigEntry<float> cPitchFight;

		private static ConfigEntry<float> cRollPitchReserve;

		private static ConfigEntry<bool> cNozIdleOpen;

		private static ConfigEntry<float> cNozRate;

		private static ConfigEntry<float> cGunAmmoStock;

		private static ConfigEntry<float> cGunAmmoBuffed;

		private static ConfigEntry<float> cFuelUse;

		private static ConfigEntry<float> cRotRate;

		private static ConfigEntry<float> cAsymReserve;

		private static ConfigEntry<float> cAsymFloor;

		private static ConfigEntry<float> cAsymFloorNeg;

		private static ConfigEntry<float> cAsymTighten;

		private static ConfigEntry<float> cAsymRelax;

		private static ConfigEntry<float> cAsymRec;

		private static ConfigEntry<float> cAsymMinIas;

		private static ConfigEntry<float> cAsymRollAcc;

		private static ConfigEntry<float> cAsymYawAcc;

		private static ConfigEntry<float> cAsymBoost;

		private static ConfigEntry<float> cAsymDead;

		private static ConfigEntry<float> cAsymFF;

		private static ConfigEntry<float> cAsymFFTau;

		private static ConfigEntry<float> cAsymRollTau;

		private static ConfigEntry<float> cAsymRelaxBoost;

		private static ConfigEntry<float> cRollPitchFF;

		private static ConfigEntry<float> cRollPitchFFAoA;

		private static ConfigEntry<float> cAsymFFBeta;

		private static ConfigEntry<float> cAsymFFAoA;

		private static ConfigEntry<float> cBetaGuard;

		private static ConfigEntry<float> cYawSatGuard;

		private static ConfigEntry<float> cLowQReg;

		private static ConfigEntry<float> cRollStop;

		private static ConfigEntry<float> cRollAccUse;

		private static ConfigEntry<float> cRollAccUseLow;

		private static ConfigEntry<float> cGndBrake;

		private static ConfigEntry<float> cGndBrakeMin;

		private static ConfigEntry<float> cGndBrakeFlap;

		private static ConfigEntry<float> cGndBrakeStab;

		private static ConfigEntry<float> cGndBrakeRud;

		private static ConfigEntry<float> cHlMaxAoA;

		private static ConfigEntry<float> cHlHighAoA;

		private static ConfigEntry<float> cGndBrakeTvc;

		private static ConfigEntry<float> cRotMinSpeed;

		private static ConfigEntry<float> cSlatStart;

		private static ConfigEntry<float> cSlatFull;

		private static ConfigEntry<float> cGndBrakeThr;

		private static ConfigEntry<float> cGUnder;

		private static ConfigEntry<float> cFFMax;

		private static ConfigEntry<float> cFFStabCost;

		private static ConfigEntry<bool> cGndBrakeSplit;


		private static ConfigEntry<string> cVentDoors;


		public static bool Enabled => cEnabled.Value;

		public static bool TelemetryEnabled => cTelemetry.Value;

		public static bool AIFlightControl => cAIFcs.Value;

		public static bool AIFlightLog => cAILog.Value;

		public static bool AIDirectRoll => cAIDirectRoll.Value;

		public static float StabRangeTEUp => Mathf.Clamp(cStabUp.Value, 0f, 45f);

		public static float StabRangeTEDown => Mathf.Clamp(cStabDn.Value, 0f, 45f);

		public static float RudderRange => Mathf.Clamp(cRudRange.Value, 0f, 45f);

		public static float FlaperonRange => Mathf.Clamp(cFlapRange.Value, 0f, 40f);

		public static float TvcRange => Mathf.Clamp(cTvcRange.Value, 0f, 20f);

		public static float TvcSlew => (cTvcSpeed.Value > 0f) ? Mathf.Clamp(cTvcSpeed.Value, 10f, 300f) : (70f * ActuatorScale);

		public static bool AIGunRollFade => cAIGunRollFade.Value;

		public static float AoAPos => cAoAPos.Value;

		public static float AoANeg => 0f - Mathf.Abs(cAoANeg.Value);

		public static float GPos => cGPos.Value;

		public static float GNeg => 0f - Mathf.Abs(cGNeg.Value);

		public static float MpoGPos => cMpoGPos.Value;

		public static float MpoGNeg => 0f - Mathf.Abs(cMpoGNeg.Value);

		public static float MaxPitchRate => cMaxPitch.Value;

		public static float RotationRate => cRotRate.Value;

		public static float ProtRate => cProt.Value;

		public static float MaxRollRate => cMaxRoll.Value;

		public static float MaxYawRate => cMaxYaw.Value;

		public static float RollGainLowIAS => cRollLo.Value;

		public static float RollGainHighIAS => cRollHi.Value;

		public static float RollAuthorityUse => cRollAuth.Value;

		public static float RollBrakeUse => cRollBrake.Value;

		public static float StabTrimCost => cStabTrim.Value;

		public static float LerxEffectiveness => cLerxEff.Value;

		public static float CgShiftAft => cCgShift.Value;

		public static float CouplingFF => cCoupFF.Value;

		public static float ReleasedPitchPriority => cRelPitchPrio.Value;

		public static float ReleasedStabRateBound => Mathf.Max(0f, cRelStabRate.Value);

		public static float RollNozzleWashoutSlew => Mathf.Max(0f, cWashSlew.Value);

		public static float ReleasedMomentLead => Mathf.Clamp(cMomLead.Value, 0f, 0.15f);

		public static float PitchOnsetGps => cOnset.Value;

		public static float StabRollCost => cStabRoll.Value;

		public static float StickSmoothing => cStickSmooth.Value;

		public static float ActuatorDamping => cActDamp.Value;

		public static float YawStopFactor => cYawStop.Value;

		public static float LowIasYawBoost => cLowYaw.Value;

		public static float LowIasYawBoostFullIas => cLowYawIas.Value;

		public static float LowIasYawBoostFalloff => cLowYawFall.Value;

		public static float TvcMoveCost => cTvcMove.Value;

		public static float RollNozzleHold => cRollTvc.Value;

		public static float RollNozzleWashout => cRollWash.Value;

		public static float RollTvcBudget => cRollTvcBudget.Value;

		public static float RollTvcBudgetCost => cRollTvcBudgetCost.Value;

		public static float RudderSideslipYawCap => cRudSideCap.Value;

		public static bool NoseWheelFbw => cNwsFbw.Value;

		public static float GroundYawRateMax => cGndYawRate.Value;

		public static float GroundLateralAccel => cGndLatAcc.Value;

		public static float GroundLineHold => cGndLine.Value;

		public static bool NozzleAnimation => cNozAnim.Value;

		public static bool NozzleAnimationAI => cNozAnimAI.Value;

		public static float AsymPitchMargin => cAsymPitchMargin.Value;

		public static bool AsymRollCoupling => cAsymRollCoupling.Value;

		public static bool RollPitchPriority => cRollPitchPriority.Value;

		public static float RollFightCost => cRollFightCost.Value;

		public static float NozzleSeamShade => cNozSeamShade.Value;

		public static float RollPitchFightCost => cPitchFight.Value;

		public static float RollPitchReserve => cRollPitchReserve.Value;

		public static bool NozzleIdleOpen => cNozIdleOpen.Value;

		public static float NozzleAreaRate => cNozRate.Value;

		public static float YawPriority => cYawPri.Value;

		public static float RollLead => cRollLead.Value;

		public static float YawRateFF => cYawFF.Value;

		public static float OuterFlapShare => cOuterFlap.Value;

		public static float StabRollShare => cStabRollShare.Value;

		public static float CoupleAoA => cCouple.Value;

		public static float PedalBeta => cPedalBeta.Value;

		public static float KBeta => cKBeta.Value;

		public static float KRoll => cKRoll.Value;

		public static float KPitch => cKPitch.Value;

		public static float KYaw => cKYaw.Value;

		public static float KpAoA => cKpAoA.Value;

		public static bool StabTvcPairedSearch => cPairTrim.Value;

		public static float LateralStabReserve => cLatRes.Value;

		public static bool StabBranchSwitch => cBranch.Value;

		public static bool OvershootPitchPriority => cOvPrio.Value;

		public static float CouplingRollLimit => cCoupRoll.Value;

		public static float StabBranchMaxAoA => cBranchMaxAoA.Value;

		public static float StabBranchTarget => cBranchTarget.Value;

		public static float StabBranchPitchTol => cBranchTol.Value;

		public static float PitchAntiWindup => Mathf.Max(0f, cAntiWind.Value);

		public static bool AoACaptureThrustBrake => cThrBrake.Value;

		public static float AoACaptureBrakeBlend => cBrakeBlend.Value;

		public static float KiAoA => cKiAoA.Value;

		public static float RudderFadeStart => cRudStart.Value;

		public static float RudderFadeEnd => cRudEnd.Value;

		public static float UnloadDecel => cUnloadDecel.Value;

		public static float UnloadRiseGain => cUnloadRise.Value;

		public static float MaxRollAccel => cRollAcc.Value;

		private static ConfigEntry<float> cDecelLook;

		private static ConfigEntry<float> cDecelStart;

		private static ConfigEntry<float> cDecelFull;

		private static ConfigEntry<float> cAsymCapDemand;

		private static ConfigEntry<float> cAsymCapSmooth;

		private static ConfigEntry<bool> cHiAoABrake;
		private static ConfigEntry<bool> cCmdShape;

		private static ConfigEntry<float> cShapeASpan;

		private static ConfigEntry<float> cShapeAKSmall;

		private static ConfigEntry<float> cShapeAKLarge;

		private static ConfigEntry<float> cShapeOnset;

		private static ConfigEntry<float> cShapeGSpan;

		private static ConfigEntry<float> cShapeGKSmall;

		private static ConfigEntry<float> cShapeGKLarge;

		private static ConfigEntry<bool> cRateBlend;

		private static ConfigEntry<float> cRateBlendAoA;

		private static ConfigEntry<float> cRateBlendStick;

		private static ConfigEntry<float> cRateBlendAoAMin;

		private static ConfigEntry<float> cRateBlendLiftG;

		private static ConfigEntry<float> cRateBlendBand;

		private static ConfigEntry<bool> cRateBlendNeg;

		private static ConfigEntry<float> cRateBlendAoANeg;

		private static ConfigEntry<float> cRateBlendAoAMinNeg;

		private static ConfigEntry<bool> cAuthCap;

		private static ConfigEntry<float> cAuthCapMargin;

		private static ConfigEntry<float> cAuthCapFloor;

		private static ConfigEntry<float> cHybridIndi;


		public static float DecelLookahead => Mathf.Max(0f, cDecelLook.Value);

		public static float DecelStart => cDecelStart.Value;

		public static float DecelFull => Mathf.Max(cDecelStart.Value + 0.5f, cDecelFull.Value);

		public static float AsymCapDemand => Mathf.Clamp(cAsymCapDemand.Value, 0.5f, 2f);

		public static float AsymCapSmoothing => Mathf.Max(0.05f, cAsymCapSmooth.Value);

		public static bool HighAoABrakeFloor => cHiAoABrake.Value;

		public static bool CmdShaping => cCmdShape.Value;

		public static float ShapeAoASpan => Mathf.Max(1f, cShapeASpan.Value);

		public static float ShapeAoAGainSmall => Mathf.Max(0.3f, cShapeAKSmall.Value);

		public static float ShapeAoAGainLarge => Mathf.Max(0.3f, cShapeAKLarge.Value);

		public static float ShapeOnsetSmall => Mathf.Clamp(cShapeOnset.Value, 0f, 1f);

		public static float ShapeGSpan => Mathf.Max(0.2f, cShapeGSpan.Value);

		public static float ShapeGGainSmall => Mathf.Max(0.3f, cShapeGKSmall.Value);

		public static float ShapeGGainLarge => Mathf.Max(0.3f, cShapeGKLarge.Value);

		public static bool HighAoARateBlend => cRateBlend.Value;

		public static float RateBlendAoA => Mathf.Clamp(cRateBlendAoA.Value, 10f, 64f);

		public static float RateBlendStick => Mathf.Clamp(cRateBlendStick.Value, 0.2f, 0.9f);

		public static float RateBlendAoAMin => Mathf.Clamp(cRateBlendAoAMin.Value, 10f, 64f);

		public static float RateBlendLiftG => Mathf.Max(1.1f, cRateBlendLiftG.Value);

		public static float RateBlendBand => Mathf.Clamp(cRateBlendBand.Value, 0.5f, 15f);

		public static bool HighAoARateBlendNeg => cRateBlendNeg.Value;

		public static float RateBlendAoANeg => Mathf.Clamp(Mathf.Abs(cRateBlendAoANeg.Value), 10f, 64f);

		public static float RateBlendAoAMinNeg => Mathf.Clamp(Mathf.Abs(cRateBlendAoAMinNeg.Value), 10f, 64f);

		public static bool LowThrustAoACap => cAuthCap.Value;

		public static float AuthCapMargin => cAuthCapMargin.Value;

		public static float AuthCapFloor => Mathf.Clamp(cAuthCapFloor.Value, 5f, 65f);

		public static float HybridIndiGain => Mathf.Clamp(cHybridIndi.Value, 0f, 1.5f);


		private static ConfigEntry<float> cYawRefKmh;

		private static ConfigEntry<float> cYawMargStart;

		private static ConfigEntry<float> cYawMargFull;

		private static ConfigEntry<float> cYawMargLow;

		public static float YawBrakeRefKmh => Mathf.Max(50f, cYawRefKmh.Value);

		public static float YawMarginStartKmh => cYawMargStart.Value;

		public static float YawMarginFullKmh => Mathf.Min(cYawMargFull.Value, cYawMargStart.Value - 1f);

		public static float YawMarginLowIas => Mathf.Max(1f, cYawMargLow.Value);

		private static ConfigEntry<float> cYawMidF;

		private static ConfigEntry<float> cYawMidLo;

		private static ConfigEntry<float> cYawMidHi;

		private static ConfigEntry<float> cYawMidRamp;

		public static float YawMidFactor => Mathf.Clamp(cYawMidF.Value, 0.2f, 2f);

		public static float YawMidLoKmh => cYawMidLo.Value;

		public static float YawMidHiKmh => Mathf.Max(cYawMidHi.Value, cYawMidLo.Value);

		public static float YawMidRampKmh => Mathf.Max(1f, cYawMidRamp.Value);

		public static bool StockPerformance => cProfile != null && cProfile.Value == PerformanceProfile.Stock;

		public static float ThrustScale => StockPerformance ? 1f : cThrust.Value;

		public static float FlareCountScale => StockPerformance ? cFlareScaleStock.Value : cFlareScaleBuffed.Value;

		public static float EWCapacityScale => StockPerformance ? cEwCapStock.Value : cEwCapBuffed.Value;

		public static float EWRechargeScale => StockPerformance ? cEwRateStock.Value : cEwRateBuffed.Value;

		public static float GunAmmoScale => StockPerformance ? cGunAmmoStock.Value : cGunAmmoBuffed.Value;

		public static float FuelUseScale => StockPerformance ? 1f : cFuelUse.Value;

		public static float DragScale => StockPerformance ? 1f : cDrag.Value;

		public static float DragReliefAoA => StockPerformance ? 0f : Mathf.Clamp(cDrAoA.Value, 0f, 0.5f);

		public static float DragReliefTransonic => StockPerformance ? 0f : Mathf.Clamp(cDrTrans.Value, 0f, 0.5f);


		public static float KpG => cKpG.Value;

		public static float KiG => cKiG.Value;

		public static float FlaperonEffectiveness => cFlaperonEff.Value;

		public static float StabilatorEffectiveness => cStabEff.Value;

		public static float ActuatorScale => cActuator.Value;

		public static bool AsymLimiter => cAsymOn.Value;

		public static bool AsymRollHeadroom => cAsymRollHead.Value;

		public static float AsymReserve => cAsymReserve.Value;

		public static float AsymCapFloor => Mathf.Abs(cAsymFloor.Value);

		public static float AsymCapFloorNeg => Mathf.Abs(cAsymFloorNeg.Value);

		public static float AsymTighten => Mathf.Max(0.1f, cAsymTighten.Value);

		public static float AsymRelax => Mathf.Max(0.1f, cAsymRelax.Value);

		public static float AsymRecoveryRate => Mathf.Max(1f, cAsymRec.Value);

		public static float AsymMinIas => cAsymMinIas.Value;

		public static float AsymRollReserveAccel => Mathf.Max(0f, cAsymRollAcc.Value);

		public static float AsymYawReserveAccel => Mathf.Max(0f, cAsymYawAcc.Value);

		public static float AsymAxisBoost => cAsymBoost.Value;

		public static float AsymDeadband => cAsymDead.Value;

		public static float AsymFeedForward => cAsymFF.Value;

		public static float AsymFFWashout => cAsymFFTau.Value;

		public static float AsymFFMaxBeta => cAsymFFBeta.Value;

		public static float AsymFFMaxAoA => cAsymFFAoA.Value;

		public static float SideslipGuardStart => cBetaGuard.Value;

		public static float YawSatGuard => Mathf.Clamp01(cYawSatGuard.Value);

		public static float LowQRegRelief => cLowQReg.Value;

		public static float RollStopFactor => cRollStop.Value;

		public static float RollAccelUse => Mathf.Clamp(cRollAccUse.Value, 0.1f, 0.95f);

		public static float RollAccelUseLowIas => Mathf.Clamp(cRollAccUseLow.Value, 0.1f, 0.95f);

		public static float GroundAeroBrake => Mathf.Clamp01(cGndBrake.Value);

		public static float GroundBrakeMinSpeed => cGndBrakeMin.Value;

		public static float GroundBrakeFlap => cGndBrakeFlap.Value;

		public static float GroundBrakeStab => cGndBrakeStab.Value;

		public static float GroundBrakeRudder => cGndBrakeRud.Value;

		public static float HighLiftMaxAoA => cHlMaxAoA.Value;

		public static float SlatAoAStart => cSlatStart.Value;

		public static float SlatAoAFull => cSlatFull.Value;

		public static float HighLiftHighAoA => Mathf.Clamp01(cHlHighAoA.Value);

		public static float GroundBrakeTvc => cGndBrakeTvc.Value;

		public static float GroundBrakeThrottle => cGndBrakeThr.Value;

		public static bool GroundBrakeLog => cGndBrakeLog.Value;

		public static bool AirbrakeSplit => cGndBrakeSplit.Value;

		public static string VentDoors => cVentDoors.Value;

		public static float RotationMinSpeed => Mathf.Max(20f, cRotMinSpeed.Value);

		public static bool RevFlowRollSense => cRevRoll.Value;

		public static float AsymRollTau => cAsymRollTau.Value;

		public static float AsymRelaxBoost => cAsymRelaxBoost.Value;

		public static float RollPitchFF => cRollPitchFF.Value;

		public static float RollPitchFFAoA => cRollPitchFFAoA.Value;

		public static float GUndershootCapture => cGUnder.Value;

		public static float RollPitchFFMax => Mathf.Max(1f, cFFMax.Value);

		public static float RollPitchFFStabCost => cFFStabCost.Value;

		public static void Init(ConfigFile cfg, ConfigFile dev, bool devUnlocked, bool devFresh, ManualLogSource log)
		{
			Log = log;
			cProfile = cfg.Bind("1. General", "PerformanceProfile", PerformanceProfile.Stock, "Stock (default) = Aryx's original thrust, drag and fuel burn. Buffed = thrust x1.08, parasitic drag x0.885, AoA / transonic drag relief 0.25 / 0.2, fuel burn x0.75. Countermeasures and gun ammo come from the matching Countermeasures section below. The flight control is the same in both. Switch before spawning: thrust, drag, countermeasures and ammo are set when a jet spawns and stay until it respawns; fuel burn follows the setting live.");
			cEnabled = cfg.Bind("1. General", "Enabled", defaultValue: true, "Replace the stock fly-by-wire with the F-22E FCS (player aircraft only).");
			cFlareScaleStock = cfg.Bind("3. Countermeasures - Stock profile", "FlareCountScale", 1f, "Flare capacity multiplier (the load at spawn and what rearming refills to). Used when PerformanceProfile = Stock. 1 = stock. All F-22Es, applied at spawn.");
			cGunAmmoStock = cfg.Bind("3. Countermeasures - Stock profile", "GunAmmoScale", 1f, "Gun (20 mm) ammunition multiplier: magazine capacity, so the load at spawn and what rearming refills to. Used when PerformanceProfile = Stock. 1 = stock. All F-22Es, applied at spawn.");
			cEwCapStock = cfg.Bind("3. Countermeasures - Stock profile", "EWCapacityScale", 1f, "Electrical storage multiplier for the EW/jammer power supply (more jamming time per charge). Used when PerformanceProfile = Stock. 1 = stock. All F-22Es, applied at spawn.");
			cEwRateStock = cfg.Bind("3. Countermeasures - Stock profile", "EWRechargeScale", 1f, "Recharge-rate multiplier for the EW/jammer power supply (engine-driven charging). Used when PerformanceProfile = Stock. 1 = stock. All F-22Es, applied at spawn.");
			cFlareScaleBuffed = cfg.Bind("4. Countermeasures - Buffed profile", "FlareCountScale", 10f, "Flare capacity multiplier (the load at spawn and what rearming refills to). Used when PerformanceProfile = Buffed. 1 = stock. All F-22Es, applied at spawn.");
			cGunAmmoBuffed = cfg.Bind("4. Countermeasures - Buffed profile", "GunAmmoScale", 2f, "Gun (20 mm) ammunition multiplier: magazine capacity, so the load at spawn and what rearming refills to. Used when PerformanceProfile = Buffed. 1 = stock. All F-22Es, applied at spawn.");
			cEwCapBuffed = cfg.Bind("4. Countermeasures - Buffed profile", "EWCapacityScale", 1.5f, "Electrical storage multiplier for the EW/jammer power supply (more jamming time per charge). Used when PerformanceProfile = Buffed. 1 = stock. All F-22Es, applied at spawn.");
			cEwRateBuffed = cfg.Bind("4. Countermeasures - Buffed profile", "EWRechargeScale", 1.2f, "Recharge-rate multiplier for the EW/jammer power supply (engine-driven charging). Used when PerformanceProfile = Buffed. 1 = stock. All F-22Es, applied at spawn.");
			cTelemetry = dev.Bind("0. Debug", "Telemetry", defaultValue: false, "Write a 50 Hz CSV of the FCS state to BepInEx/F22E_FCS_<time>.csv.");
			cAIFcs = cfg.Bind("2. AI", "AIFlightControl", defaultValue: true, "AI-flown F-22Es (simulated on this machine, no human pilot) also fly the FCS: everything the player gets (allocator, TVC, flaps, limits, damage protection) except the pitch law, which is pitch-rate command with the stock fly-by-wire's own stick-to-rate gain (so the AI's autopilot tuning still fits), inside the FCS AoA/G limits. Telemetry is never written for AI jets. false = AI F-22Es fly the stock fly-by-wire on the modified airframe (pre-1.8.22).");
			cAILog = dev.Bind("0. Debug", "AIFlightLog", defaultValue: false, "Every 20 s, log one summary line per AI F-22E the FCS is flying (mode, IAS, AoA and g range, time in AoA protection), so AI jets can be checked from BepInEx/LogOutput.log after any mission. Each F-22E also logs once which control path it takes when it first reaches the flight-control hook.");
			cAIDirectRoll = cfg.Bind("2. AI", "AIDirectRoll", defaultValue: true, "AI F-22Es only: the AI's roll stick drives the velocity-vector roll rate almost directly (up to the jet's roll authority, still capped by the yaw authority at high AoA), like the stock fly-by-wire, instead of through the player's smooth roll trajectory. The AI's roll autopilot is a PID tuned for the stock response; the extra lag of the smooth trajectory made it overshoot and roll back and forth. false = 1.8.24 behaviour.");
			cAIGunRollFade = cfg.Bind("2. AI", "AIGunRollFade", defaultValue: true, "AI F-22Es only: while the AI points its nose (guns) at a moving target, its roll stick is faded out when the aim error is under ~6 deg (none under 1 deg). Near the aim point the AI's 'put the lift vector on the target' roll logic has no stable answer and flips between rolling left and right every second; pitch and rudder close the last few degrees instead. false = off.");
			cAoAPos = dev.Bind("2. Envelope", "AoALimitPositive", 65f, "AoA limit (deg) in AoA/G command and gear-down rate command.");
			cAoANeg = dev.Bind("2. Envelope", "AoALimitNegative", 45f, "Negative AoA limit magnitude (deg).");
			cGPos = dev.Bind("2. Envelope", "GLimitPositive", 9.5f, "Positive load limit (g), normal law.");
			cGNeg = dev.Bind("2. Envelope", "GLimitNegative", 3.5f, "Negative load limit magnitude (g), normal law.");
			cMpoGPos = dev.Bind("2. Envelope", "MPOGLimitPositive", 12.5f, "Positive load limit (g) with Stability Assist OFF (MPO).");
			cMpoGNeg = dev.Bind("2. Envelope", "MPOGLimitNegative", 3.5f, "Negative load limit magnitude (g) in MPO.");
			cMaxPitch = dev.Bind("2. Envelope", "MaxPitchRate", 48f, "Pitch-rate budget ceiling (deg/s).");
			cProt = dev.Bind("2. Envelope", "AoARecoveryRate", 48f, "AoA-protection recovery rate (deg/s), independent of thrust and weight.");
			cMaxRoll = dev.Bind("3. Lateral-directional", "MaxRollRate", 250f, "Absolute body roll-rate ceiling (deg/s). Below ~250 m/s IAS the original-aircraft schedule and RollAuthorityUse set the rate; above it this ceiling holds the rate flat.");
			cMaxYaw = dev.Bind("3. Lateral-directional", "MaxYawRate", 75f, "Body yaw-rate limit (deg/s).");
			cRollLo = dev.Bind("3. Lateral-directional", "RollRateGainLowIAS", 1.2f, "Roll-rate ceiling vs the ORIGINAL aircraft's roll rate below ~120 m/s IAS (blends to the high-IAS gain by 170 m/s).");
			cRollHi = dev.Bind("3. Lateral-directional", "RollRateGainHighIAS", 1.1f, "Roll-rate ceiling vs the original aircraft's roll rate above ~170 m/s IAS.");
			cRollBrake = dev.Bind("3. Lateral-directional", "RollBrakeUse", 0.8f, "Fraction of the full roll authority (wings and stabilators) used to stop a roll, on top of roll damping. Before 1.8.26 this was a fraction of the wing-only authority.");
			cRollAuth = dev.Bind("3. Lateral-directional", "RollAuthorityUse", 1f, "Steady roll never needs more than this fraction of the available roll control moment (roll rate is given up instead of saturating the surfaces).");
			cCouple = dev.Bind("3. Lateral-directional", "CoupledAxisAoA", 36f, "Above this |AoA| the pedals blend into the stick's velocity-vector roll (full coupling 14 deg higher).");
			cPedalBeta = dev.Bind("3. Lateral-directional", "PedalSideslip", 15f, "Sideslip commanded by full pedal below the coupling AoA (deg).");
			cRudStart = dev.Bind("3. Lateral-directional", "RudderFadeStartAoA", 35f, "Rudders start fading out above this |AoA| (deg) - vertical-tail blanking, as on the real F-22.");
			cRudEnd = dev.Bind("3. Lateral-directional", "RudderFadeEndAoA", 50f, "Rudders fully faded (held neutral) above this |AoA| (deg), apart from RudderSideslipYawCap.");
			cRudSideCap = dev.Bind("3. Lateral-directional", "RudderSideslipYawCap", 2f, "1.8.35: past the rudder fade, the canted rudders' inverse roll is used to correct sideslip (at high AoA a body roll moves sideslip; from ~55 deg AoA the rudders make >3x more sideslip than velocity-vector roll, where the flaperons and stabs have lost theirs). They may only deflect in the sideslip-reducing direction, scaled in from 0.5 to 3 deg of sideslip error, and only as far as keeps the body yaw acceleration they make within this many deg/s^2 (their yaw per degree falls to ~0 by 80 deg AoA, so the travel opens up as AoA rises). 0 = off (1.8.34: rudders neutral above RudderFadeEndAoA).");
			cRollAcc = dev.Bind("3. Lateral-directional", "MaxRollAccel", 720f, "Ceiling on the shaped velocity-vector roll acceleration (deg/s^2); below it the measured authority sets the rate.");
			cBetaGuard = dev.Bind("3. Lateral-directional", "SideslipGuardStart", 4f, "Sideslip error (deg) above which the velocity-vector roll/yaw command is backed off, fading to zero command 10 deg above it. Raise it for manoeuvres that live at large sideslip on purpose (falling leaf); lower it for tighter coordination.");
			cYawSatGuard = dev.Bind("3. Lateral-directional", "YawSaturationGuard", 0.7f, "How hard a yaw-saturated allocation backs the roll/yaw command off (0..1, fraction removed at full saturation). Lower = the command stays up when the surfaces cannot fully deliver it (more sideslip, more yaw rate); 0 = off.");
			cKBeta = dev.Bind("4. Gains", "SideslipGain", 1.3f, "Stability-axis yaw rate per degree of sideslip error (1/s).");
			cKRoll = dev.Bind("4. Gains", "RollRateGain", 6f, "Roll-rate loop bandwidth (1/s).");
			cKPitch = dev.Bind("4. Gains", "PitchRateGain", 5f, "Pitch-rate loop bandwidth (1/s).");
			cKYaw = dev.Bind("4. Gains", "YawRateGain", 2.5f, "Yaw-rate loop bandwidth (1/s).");
			cBrakeBlend = dev.Bind("4. Gains", "AoACaptureBrakeBlend", 0.3f, "How far the capture braking plan leans from the authority at the target AoA (0) toward what is available right now (1). Lower = earlier braking, less overshoot, slower AoA build; higher = faster AoA build, more overshoot below afterburner. 0.3 is the bench balance.");
			cThrBrake = dev.Bind("4. Gains", "AoACaptureThrustBrake", defaultValue: true, "AoA capture plans its braking with the nose-down authority it can count on at the TARGET AoA: the nozzles' full authority at the live thrust (so less below afterburner and much less at part throttle) plus the stabs' authority at the target AoA from the surface model (almost none past ~45 deg, where they stall). The approach follows sqrt(2 x 0.7 x that x distance), the same deceleration the pitch trajectory can deliver. false = pre-1.8.20.");
			cAntiWind = dev.Bind("4. Gains", "AoAGAntiWindup", 4f, "Back-calculation anti-windup for the AoA and G integrators (1/s): whenever a limiter (AoA/G capture braking, AoA limit, pitch-rate budget) clips the pitch-rate command below what the AoA/G loop asks for, the integrator is pulled back toward the command at this rate. Without it the integrator winds up during every capture and then holds AoA about 0.2 deg above the command until it unwinds (several seconds). 0 = pre-1.8.20.");
			cLatRes = dev.Bind("4. Gains", "LateralStabReserve", 0.7f, "While you hold pitch stick AND lateral stick (roll or pedal) together, the pitch law builds pitch rate with the nozzles' authority plus only (1 - this x |lateral stick|) of the stabs' remaining authority, and the allocator's cost for symmetric (pitch) stab deflection rises with it, so the nozzles take the pitch and the stabs stay for roll/yaw. Braking toward the commanded AoA/G, the AoA/G limits and protection always use full authority. No lateral stick, or pitch stick released = unchanged. 0 = pre-1.8.19.");
			cPairTrim = dev.Bind("4. Gains", "StabTvcPairedSearch", defaultValue: true, "The allocator also tries moving each stabilator with the nozzles taking up its pitch change, and keeps the pair move when it lowers the total error. Without it, a stab parked on a stalled, flat part of its curve for pitch cannot be moved to where it makes roll, because moving it alone breaks pitch and moving the nozzles alone breaks it the other way. Lets the nozzles carry the pitch trim whenever they have room, freeing the stabs for roll/yaw. false = pre-1.8.18.");
			cBranch = dev.Bind("4. Gains", "StabBranchSwitch", defaultValue: true, "When a stabilator has been driven deep past its stall (local AoA more than 10 deg past the peak) during roll/yaw at moderate AoA, move it to the attached-flow side of its lift peak instead (trailing edge up, where the lift curve is steep), hold it there, and re-allocate everything else around it, the nozzles taking up the pitch. Undone when the modelled pitch error would grow by more than StabBranchPitchTol. false = pre-1.8.21.");
			cOvPrio = dev.Bind("4. Gains", "OvershootPitchPriority", defaultValue: true, "While the nose is pitching up faster than the pitch law commands above ~15-25 deg AoA (an AoA run-away), or AoA protection is active: (1) the allocator keeps pitch priority instead of handing stalled stabs to roll/yaw, and (2) it models the nose-down a stab really loses when it is driven deep past its stall for roll/yaw (the monotone stab pitch model hides that loss). false = pre-1.8.23.");
			cCoupRoll = dev.Bind("4. Gains", "CouplingRollLimit", 0.5f, "While AoA is building past 20-30 deg (or running away), cap the velocity-vector roll rate so the inertial pitch-up it causes, (Izz-Ixx)/Iyy * p * r, stays within this fraction of the nose-down authority the jet has at that AoA (aero + stabs + nozzles at live thrust). This is what let full-aft + full-roll pulls overshoot to 70-77 deg below afterburner. Held-AoA rolls are not affected. 0 = off (pre-1.8.23).");
			cBranchMaxAoA = dev.Bind("4. Gains", "StabBranchMaxAoA", 42f, "Body AoA (deg) above which StabBranchSwitch is not used. Above ~44 deg the stalled side of the stab is the better one for roll/yaw.");
			cBranchTarget = dev.Bind("4. Gains", "StabBranchTarget", 20f, "Local AoA (deg) StabBranchSwitch moves the stab to, on the attached side of its lift peak.");
			cBranchTol = dev.Bind("4. Gains", "StabBranchPitchTol", 5f, "Largest growth in modelled pitch-acceleration error (deg/s^2) StabBranchSwitch accepts. Protects AoA holding when the nozzles cannot take up the stab's pitch.");
			cKpAoA = dev.Bind("4. Gains", "AoAGain", 5f, "AoA command: deg/s of pitch rate per deg of AoA error.");
			cKiAoA = dev.Bind("4. Gains", "AoAIntegral", 3f, "AoA command integral gain (1/s) near the target.");
			cStickSmooth = cfg.Bind("1. General", "StickSmoothing", 0.06f, "Time constant (s) of the first-order smoothing on pitch/roll/pedal inputs. 0 = off.");
			cActDamp = dev.Bind("5. Airframe", "ActuatorDamping", 0.06f, "Actuator smoothing (s): time for a servo command to accelerate to full servo speed (and brake into its target on a matching curve). Servo speed is unchanged and a steady slew runs at full speed. 0 = off.");
			cYawStop = dev.Bind("3. Lateral-directional", "YawStopFactor", 0.7f, "Above 15-25 deg AoA the body yaw rate is capped at this x measured yaw authority (so the yaw can be stopped again). Higher = more responsive moderate-AoA rolls, more sideslip on stops.");
			cRollLead = dev.Bind("3. Lateral-directional", "RollLagCompensation", 0.008f, "Roll command lead per unit of roll damping (s per 1/s above 2.5/s): cancels the roll loop's tracking lag at high dynamic pressure so roll stops end when the command does, without a slow tail. 0 = off.");
			cYawPri = dev.Bind("4. Gains", "YawAxisPriority", 8f, "Allocator weight of the yaw axis relative to roll below ~20 deg AoA. Higher = rudders keep the roll coordinated even when their roll side-effect costs some roll acceleration.");
			cYawFF = dev.Bind("4. Gains", "YawRateFeedForward", 0.5f, "Fraction of the yaw-rate command's rate of change fed forward to the yaw loop at low AoA below 200 m/s IAS (blends to 1.0 by 300 m/s IAS, and by 28 deg AoA, where the body yaw is the velocity-vector roll). Lower = calmer rudders at low speed, slightly more sideslip.");
			cLowYaw = dev.Bind("3. Lateral-directional", "LowIasYawBoost", 1.7f, "Above 15-25 deg AoA, widens the yaw-rate cap (YawStopFactor x yaw authority, and its 20 deg/s floor) by this factor at very low IAS (falling leaf). It fades out along a curve above LowIasYawBoostFullIAS (see LowIasYawBoostFalloff). 1 = off.");
			cLowYawIas = dev.Bind("3. Lateral-directional", "LowIasYawBoostFullIAS", 70f, "IAS (m/s) up to which the full LowIasYawBoost applies.");
			cLowYawFall = dev.Bind("3. Lateral-directional", "LowIasYawBoostFalloff", 20f, "Above LowIasYawBoostFullIAS the extra margin decays exponentially with this IAS scale (m/s): 10 = ~50 % of it left at +7 m/s, ~10 % at +23 m/s, gone by 120 m/s.");
			cTvcMove = dev.Bind("4. Gains", "TvcMoveCost", 2E-05f, "Allocator cost of moving the TVC nozzles. 2e-5 = nozzles take pitch changes first; ~0.02 = stabs do the quick pitch changes and the nozzles slowly take over the sustained (trim) share. Post-stall the nozzles are always fast.");
			cGndBrake = dev.Bind("Ground", "GroundAeroBrake", 1f, "Aerodynamic braking on the rollout: flaperons and ailerons up, stabilators up, rudders split symmetrically for drag without yaw. Needs weight on the main wheels and either the wheel brake or throttle below 25 %, so it never appears during a take-off roll. 0 = off.");
			cGndBrakeMin = dev.Bind("Ground", "GroundAeroBrakeMinSpeed", 0f, "TAS (m/s) below which the aero-brake stays stowed. 0 = it is out whenever the other conditions are met, so you can see it at a standstill; it still scales up with speed.");
			cGndBrakeThr = dev.Bind("Ground", "GroundAeroBrakeThrottle", -1f, "Optional second trigger: throttle below this (0..1) also deploys the aero-brake. NEGATIVE (the default) disables it, so only the wheel brake deploys it, at any throttle.");
			cGndBrakeSplit = dev.Bind("Ground", "AirbrakeSplitInFlight", defaultValue: true, "In the AIR, the brake control deploys the airframe's split surfaces (the stock speedbrake mechanism). On the ground they stay stowed and the airbrake is the rollout surface configuration alone. The stock rule deployed them whenever the throttle was at idle, airborne as well as on the ground; the FCS drives them now, so idle throttle deploys nothing. false = split surfaces never deploy.");
			cVentDoors = dev.Bind("6. Visuals", "VentDoors", "Bypass", new ConfigDescription("The six gill doors on the upper fuselage. Bypass (1.8.37 default) = inlet bypass doors: they dump the inlet air the engines are not taking - open with speed when the throttle is back (from about Mach 0.35 with engines off, Mach 0.7-1.1 at idle, only above ~Mach 1.3 at MIL/AB), close as the engines spool up and above ~12-24 deg AoA; the brake does nothing to them. Airbrake = they open with the brake (1.8.36). Stock = Aryx's throttle/speed rule.", new AcceptableValueList<string>("Bypass", "Airbrake", "Stock")));
			cGndBrakeLog = dev.Bind("0. Debug", "GroundAeroBrakeLog", defaultValue: false, "Log one line a second while on the ground showing every gate the aero-brake checks (mode, wheels, speed, brake, throttle) and the resulting blend. Turn it off once it behaves.");
			cGndBrakeFlap = dev.Bind("Ground", "GroundAeroBrakeFlaperon", 20f, "Flaperon / aileron trailing-edge-up deflection (deg) in the aero-brake, clamped to their travel.");
			cGndBrakeStab = dev.Bind("Ground", "GroundAeroBrakeStabilator", 26f, "Stabilator trailing-edge-up deflection (deg) in the aero-brake - it also holds the nose up while the mains carry the jet.");
			cGndBrakeTvc = dev.Bind("Ground", "GroundAeroBrakeTVC", 10f, "Nozzle deflection (deg) in the aero-brake, in the same sense as aft stick (thrust up at the tail), so the nozzles sit up with the stabilators. Clamped to the nozzle travel.");
			cRotMinSpeed = dev.Bind("Ground", "RotationMinSpeed", 45f, "The take-off/landing rotation law (closed-loop pitch with the mains on the ground) only arms above this TAS (m/s). It used to arm at 15 m/s, an ordinary taxi speed, where it flew closed-loop pitch against the ground constraint and threw the surfaces about. It disarms 8 m/s below this.");
			cGndBrakeRud = dev.Bind("Ground", "GroundAeroBrakeRudder", 25f, "Symmetric rudder split (deg) in the aero-brake. Negative flips which way they toe.");
			cNwsFbw = dev.Bind("Ground", "NoseWheelFbw", true, "1.8.36: fly-by-wire ground steering. Pedal commands a yaw rate (GroundYawRateMax at taxi speed, limited to GroundLateralAccel of turn at speed); pedal released = yaw braking to zero rate, then the jet holds a straight line along the track it had (a crab/drift at touchdown is steered out, the heading lined up with the track). The nose wheel is steered by the FCS (kinematic feed-forward + yaw-rate feedback) and, in the ground roll, the rudders join the yaw-rate loop. Below 1.5 m/s the pedal steers the wheel directly (pivoting). false = stock: pedal sets the nose-wheel angle, rudders follow the pedal.");
			cGndYawRate = dev.Bind("Ground", "GroundYawRateMax", 15f, "Full-pedal yaw rate on the ground (deg/s) at taxi speed.");
			cGndLatAcc = dev.Bind("Ground", "GroundLateralAccel", 2.5f, "Full pedal never asks for more than this lateral acceleration (m/s^2) in a ground turn: the yaw-rate command is min(GroundYawRateMax, this / ground speed). 2.5 = full pedal gives 2 deg/s at 260 km/h.");
			cGndLine = dev.Bind("Ground", "GroundLineHold", 1f, "Pedal released: how strongly the jet steers back onto the straight line it was on (1 = full, 0 = hold the track direction only and accept the sideways offset a gust or touchdown drift left).");
			cNozAnim = dev.Bind("6. Visuals", "NozzleAnimation", true, "1.8.37/1.8.40: the two nozzle flaps move separately (visual only - thrust and vectoring physics are unchanged). Each flap is a root piece under the shroud, a forward segment (the flap's own cowl, hinged at its front edge) and an end segment; seam strips close the joints and nothing stretches. Vectoring: cowl and end segment move as one; the flap swinging outward is pulled in (up to 3 cm) and pivots at the inner surface of its root joint, so its cowl rides over the root; the one swinging inward rotates about the same point without the pull-in. Afterburner: flaps open 1.5 deg. MIL: flaps rotated 7.7 deg in. Applied when a jet spawns. false = Aryx's original one-piece nozzle.");
			cNozSeamShade = dev.Bind("6. Visuals", "NozzleSeamShade", 0.07f, "1.8.42: brightness (0-1, sRGB grey) of the two seam bands in each nozzle flap (the gaps between the root, the floating cowl and the end flap). Plain material like the cavity parts: 0.035 = the black liners, 0.085 = the dark grey beams. Applied when a jet spawns. 1.8.40/41 used Aryx's atlas grey (reads about 0.2-0.3 through the livery).");
			cPitchFight = dev.Bind("4. Gains", "RollPitchFightCost", 1f, "1.8.42: while you roll, a stabilator pitching the nose up (trailing edge up) while the nozzles pitch it down to cancel costs the allocator this x its pitch acceleration squared (rad/s^2), so it rolls with less stab trailing-edge up and the nozzles keep their nose-down travel instead of the roll rate. Near the stabs' roll reversal (the trailing-edge-down stab stalled) one stab went fully trailing-edge up with the other near neutral and the nozzles pinned nose-down. Only that pattern counts (stabs nose-down with nozzles nose-up, the roll-pitch-priority trim, is left alone); off while the pitch-rate command is moving, while the nose comes up faster than commanded, in AoA protection and on AoA run-away. Bench vs 1.8.41: reversals at 19 deg AoA / 300 km/h nozzle min -20 -> -13, AoA over 1.2 -> 0.5 deg; roll stop at 200 km/h -14 -> -10; 2.5 t store at 600 km/h -20 -> -12, over 2.9 -> 0.9; at 26 deg a stab past 35 deg local AoA 32 % -> 0 % of the roll. Costs: reversal at 13 deg / 600 km/h over 3.9 -> 8.3 deg, 10 deg / 300 km/h 1.1 -> 2.7; bank at 1 s at 19-22 deg AoA 42 -> 36 and 28 -> 20 deg. 0 = off (1.8.41).");
			cNozAnimAI = cfg.Bind("2. AI", "NozzleAnimationAI", false, "1.8.40: animate AI jets' nozzles too. false = AI F-22Es keep Aryx's one-piece nozzle (no skinning or per-frame posing for them); player jets, including other players online, are animated.");
			cNozIdleOpen = dev.Bind("6. Visuals", "NozzleIdleOpen", true, "Nozzle area schedule in dry power: true = open at idle and closing toward MIL (how fighter engine nozzles are scheduled, to spoil idle thrust); false = closed from idle to MIL. Afterburner opens it either way.");
			cNozRate = dev.Bind("6. Visuals", "NozzleAreaRate", 1.2f, "How fast the nozzle area moves (full travel, closed to full-afterburner, per second).");
			cRotRate = dev.Bind("2. Envelope", "RotationRate", 8f, "Takeoff / landing rotation (main wheels on the ground, nose wheel up): full stick commands this pitch rate (deg/s) and neutral stick holds the nose attitude.");
			cRollTvc = dev.Bind("4. Gains", "RollNozzleHold", 0f, "1.8.31: default 0 (the handover kept both stabs trailing-edge down for whole roll cycles and bounced the nozzles in roll spam; with ±20 deg the nozzles now take the roll trim). Low-AoA rolls with no pitch command: the stabs take over the roll surfaces' pitch-moment change so the TVC stays near its pre-roll trim. Below ~150 m/s IAS the nozzles still catch the fast part and hand the sustained part to the stabs as far as the stabs have travel to spare; above it the nozzles simply hold still. 1 = full, 0 = off (nozzles trim the roll alone, 1.7.0 behaviour).");
			cAsymPitchMargin = dev.Bind("4. Gains", "AsymPitchMargin", 20f, "1.8.37: with an asymmetric load (lateral CG offset above ~8 cm) the asymmetric-load AoA cap is also held below the AoA where the jet runs out of nose-down authority - full nose-down stabs plus the nozzles at the thrust you have - minus this margin (deg/s^2), less 2 deg. On Aryx's airframe that point is only ~18-19 deg AoA at idle (the stabs stall trailing-edge down), so at idle the old cap (up to ~21 deg) sat right on the edge and any overshoot pitched up into a departure. Throttle up and the nozzles raise it again. Never below 10 deg. 0 = off (1.8.36).");
			cAsymRollCoupling = dev.Bind("4. Gains", "AsymRollCoupling", true, "1.8.37: with an asymmetric load, the roll-coupling limit (CouplingRollLimit) also works at a held AoA, not just while AoA is building past 12-22 deg, and pitch gets priority over roll as soon as AoA overshoots the asymmetric-load cap. Rolling toward the heavy wing at 12-20 deg AoA and idle used to reach 100-160 deg/s and then pitch up past the cap it could not stop in. Clean jets are unaffected. false = 1.8.36.");
			cRollPitchPriority = dev.Bind("4. Gains", "RollPitchPriority", true, "1.8.40: while you roll (below ~18-26 deg AoA), the nozzles stay within RollTvcBudget of their pre-roll trim and the pitch moment the roll makes is taken by the stabilators, with roll rate given up where they cannot. The allocation is re-solved with the flaperons' true pitch moment and the stabs kept on the attached side of their lift peak (past it the down-going stab turns nose-down into pitch-up), pitch and yaw weighted 10x over roll. Accepted only if the modelled pitch error stays within 4 deg/s^2 of what free nozzles would give; otherwise the band widens (x3, x8) or the normal allocation is kept. The nozzles are let go in AoA protection / overshoot past the AoA or asymmetric-load limit and briefly on roll stops and reversals. Bench cost: up to ~20 % roll rate at 15-20 deg AoA, ~35 % in a 1 g roll at 250 km/h (19 deg AoA); none above ~350 km/h at 1 g. RollTvcBudget sets the band: raise it to trade nozzle travel back for roll rate. false = 1.8.39.");
			cRollFightCost = dev.Bind("4. Gains", "RollFightCost", 5f, "1.8.41: while the pedals are in (and 1 s after), the allocator may not split the horizontal surfaces against each other: a group (stabilators, outboard flaperons, inboard flaperons) whose roll opposes the net roll of the others costs this x its roll acceleration squared. With full pedal the rudders reach their stops and the allocator used to buy a few deg/s^2 of yaw from the flaperons' drag by rolling them one way and the stabs the other (roll cancelled) - the large, reversing stab/flaperon deflections on pedal inputs, and all of the yaw once the vertical tails are shot off. Rolls without pedal are allocated exactly as in 1.8.40. Bench, full pedal at 340 km/h: stab-vs-flaperon roll fight mean 24 -> 4 deg/s^2; tails shot off: 79 -> 9. 0 = off (1.8.40).");
			cRollPitchReserve = dev.Bind("4. Gains", "RollPitchReserve", 0.15f, "1.8.41: while you roll, the roll-rate limit and roll-entry acceleration yield when the TVC nozzles have less than this fraction of their nose-down travel (nose-up at negative AoA) left at the live thrust, and come back as the margin returns. Above the RollPitchPriority band (18-26 deg AoA) the stabs lose nose-down authority when they roll (the down-going stab stalls) and the nozzles carry the roll's pitch-up alone: they sat on the -20 deg stop for whole rolls at 22-32 deg AoA, with or without a store. Bench, full roll at 26 deg AoA, AB, 450 km/h: nozzles on the stop 85 % -> 26 % of the roll, bank at 2 s 110 -> 87 deg; with an 800 kg store at the 22 deg cap: 35 % -> 18 %, 87 -> 77 deg. No change below ~18 deg AoA. Higher = more nozzle margin, less roll rate. 0 = off (1.8.40).");
			cRollTvcBudget = dev.Bind("4. Gains", "RollTvcBudget", 1.5f, "1.8.34: 1.5 (1.8.33: 2, 1.8.32: 4). While you roll or yaw, the TVC nozzles stay within this many degrees of where they were before the lateral input (the pitch trim for your own pitch command), and the stabilators take the pitch moment the roll itself makes (mostly the flaperons' lift change). It is an allocator cost, not a clamp: if the stabs cannot supply it (on a stop), the nozzles still go further. The limit opens up to full nozzle travel between 14 and 20 deg AoA (the down-going stab is then near its stall and nose-down from it costs roll rate), between T/W 0.7 and 1.1 (with afterburner the roll trim is only a few degrees and the nozzles stay the fast pitch effector), in AoA protection / run-away and while the pitch-rate command is moving. Cost on the bench: 3-10 % roll rate in 300-350 km/h part-throttle roll spam, about 1 % bank at 1 s in single rolls. 0 = off (1.8.31: the nozzles take the whole roll trim, down to -20 deg at part throttle).");
			cRollTvcBudgetCost = dev.Bind("4. Gains", "RollTvcBudgetCost", 8f, "1.8.34: 8 (1.8.33: 6, 1.8.32: 2). Allocator cost per (degree / 10)^2 of nozzle travel beyond RollTvcBudget. Higher = a harder budget.");
			cRollWash = dev.Bind("4. Gains", "RollNozzleWashout", 0.4f, "Time constant (s) separating 'fast' (nozzles) from 'sustained' (stabs) in RollNozzleHold. Lower = the stabs take over sooner.");
			cSlatStart = dev.Bind("5. Airframe", "SlatAoAStart", 2f, "AoA (deg) at which the slats start coming down. The high-lift device has no aero model beyond the wing area it adds, and the stock AoA term is a step rather than a schedule, so the FCS drives it off AoA directly.");
			cSlatFull = dev.Bind("5. Airframe", "SlatAoAFull", 20f, "AoA (deg) at which the slats are fully down. They then stay down to HighLiftMaxAoA.");
			cHlMaxAoA = dev.Bind("5. Airframe", "HighLiftMaxAoA", 70f, "The slats (high-lift device) stay deployed up to this |AoA|, fading over the 10 deg below it. They used to be tied to the flaperon's trailing-edge-down schedule and so retracted by 30 deg AoA. The device's mechanical TE-down swing is subtracted by the flap drive, so the net flaperon bias is unchanged: only the slats and the wing area they add stay out.");
			cHlHighAoA = dev.Bind("5. Airframe", "HighLiftHighAoAShare", 1f, "How much of the slat deployment is kept at high AoA (0..1). 0 = pre-1.8.0 behaviour (slats retract with the flap schedule).");
			cOuterFlap = dev.Bind("5. Airframe", "OuterFlaperonDroop", 0.6f, "Outer flaperons droop with the flap schedule by this fraction of the inboard flap angle (stall-capped, roll-biased).");
			cStabRoll = dev.Bind("4. Gains", "StabilatorRollCost", 0.0005f, "Allocator cost of differential (roll) stabilator use at attached flow. Lower = stabs do more of the roll.");
			cStabRollShare = dev.Bind("4. Gains", "StabilatorRollShare", 0.55f, "Share of the differential-stab roll moment counted as usable roll authority (sets roll rate ceiling / acceleration).");
			cOnset = dev.Bind("4. Gains", "PitchOnsetLimit", 150f, "High-speed pitch pacing: pitch-rate command acceleration <= this / lift slope (g per deg). Lower = gentler at high dynamic pressure.");
			cRollPitchFF = dev.Bind("4. Gains", "RollPitchFeedForward", 1f, "Corrects the pitch target by the pitch moment the roll surfaces are about to make (their pitch side-effect is not in the allocator's model), so the stabilators and nozzles move with a roll instead of chasing it afterwards. Mostly visible at low IAS, where the nozzles are the fast pitch effector and the chase shows as nozzle twitch in rolls. 0 = off (pre-1.7.12 behaviour).");
			cLowQReg = dev.Bind("4. Gains", "LowQAllocatorRelief", 1f, "Scales the allocator's regularisation (neutral preference, move cost, pair costs) with dynamic pressure squared, capped at the high-speed value. The cost's error term grows with q^2 and the regularisation did not, so below ~150 m/s the surfaces were held near neutral and near their last position: weak roll and sluggish reversals at low IAS. 0 = pre-1.8.0 behaviour.");
			cRollStop = dev.Bind("3. Lateral-directional", "RollStopFactor", 1f, "Roll-rate ceiling is also limited to this many seconds of the measured roll acceleration (the roll equivalent of YawStopFactor). Since roll acceleration is moment / inertia, this is what makes the roll-rate limit follow a heavy load, especially one far from the CG. 0 = off.");
			cRollAccUse = dev.Bind("3. Lateral-directional", "RollAccelUse", 0.5f, "Share of the roll authority planned for acceleration at and above ~110 m/s IAS (the rest is reserve for regulation against roll damping).");
			cRollAccUseLow = dev.Bind("3. Lateral-directional", "RollAccelUseLowIAS", 0.8f, "Same at and below 60 m/s IAS, blending to RollAccelUse by 110. Roll damping is small down there and the surfaces are far from saturation, so more of the authority can go into the manoeuvre - this is what makes a low-speed reversal crisp.");
			cFFMax = dev.Bind("4. Gains", "RollPitchFeedForwardMax", 30f, "Ceiling on the roll->pitch feed-forward, in deg/s^2 of pitch acceleration. It is an anticipation rather than a measurement, and at high dynamic pressure the roll surfaces' pitch moment is large enough that an error in it moves real g.");
			cFFStabCost = dev.Bind("4. Gains", "RollPitchFeedForwardStabCost", 1f, "During the feed-forward pass, symmetric (pitch) stabilator deflection costs this much more, so the correction lands on the nozzles instead of showing up as a large mean stabilator deflection under the roll's differential. 1 = no preference.");
			cRollPitchFFAoA = dev.Bind("4. Gains", "RollPitchFeedForwardMaxAoA", 35f, "RollPitchFeedForward fades out over the 15 deg below this |AoA| and is off above it: post-stall the stabilators have no pitch authority to redistribute and the pass would only compete for the surfaces holding the jet in yaw.");
			cCoupFF = dev.Bind("4. Gains", "InertialCouplingFF", 0f, "Feed-forward of the gyroscopic roll/pitch/yaw coupling (w x Iw) between commanded and present rates. 0 = off (default since 1.8.14: evaluated at the commanded rates it arrives as a nose-down step the instant you roll, before the jet has the rates; the INDI loop already measures the real coupling).");
			cRelPitchPrio = dev.Bind("4. Gains", "ReleasedStickPitchPriority", 30f, "Stick released (zero-rate hold): when the pitch rate starts to leave the hold, the allocator weights pitch this many times harder against roll/yaw for the stabilators, so a roll cannot pull them into a nose-down shove. Scales in with the pitch-rate error (1 to 9 deg/s) and out with stick (fully off from 18% stick) and with stabilator stall. 1 = off (pre-1.8.14).");
			cRelStabRate = dev.Bind("4. Gains", "ReleasedStickStabRateBound", 0.1f, "Stick released: each allocation may only ask the stabilators for what their servos reach in this many seconds, so the nozzles and wing surfaces are never placed against a stabilator position that is still a third of a second away. Relaxes with stick and stabilator stall. 0 = off (pre-1.8.14).");
			cWashSlew = dev.Bind("4. Gains", "RollNozzleWashoutSlew", 5f, "RollNozzleHold hands the nozzles' pitch trim to the stabs during rolls. While a stab is on its stop that handover is blocked and builds up; this caps how fast it is then applied (deg of nozzle per second), so it cannot all land in one frame the moment the stabs come off the stop - which is when they are braking the roll. 0 = uncapped (pre-1.8.15).");
			cMomLead = dev.Bind("4. Gains", "ReleasedStickMomentLead", 0.035f, "1.8.31: 0.035 (was 0.07; with any control delay 0.07 rang for 1-2 s after a pitch stop). Stick released, zero-rate hold established: the INDI pitch loop leads its measured effector moment by this many seconds of its own rate of change, covering the servo/filter lag when the trim moment is ramping (AoA bleeding off after a hard pull). Fades in over 0.2 s; off with stick, while the pitch-rate command is moving, in AoA protection and with stab stall. 0 = off (pre-1.8.16). Above ~0.1 s it starts to wobble.");
			cStabTrim = dev.Bind("4. Gains", "StabilatorTrimCost", 0.004f, "Allocator cost of symmetric (pitch) stabilator deflection relative to the nozzles. Higher = steady trim / sustained pitch rate carried by TVC, stabs kept for transients and whatever TVC cannot hold.");
			cUnloadDecel = dev.Bind("4. Gains", "UnloadDecel", 20f, "Easing the stick: pitch-rate deceleration (deg/s^2) toward the rate the aircraft will sustain at the new AoA/G. Lower = smoother.");
			cUnloadRise = dev.Bind("4. Gains", "UnloadRiseGain", 1.5f, "Easing the stick: if AoA still rises during the decel, the pitch-rate target drops by this x AoA rate (to zero at most).");
			cGUnder = dev.Bind("4. Gains", "GUndershootCapture", 1f, "Easing out of a hard pull at high dynamic pressure, the pitch rate is bounded so the load is captured at the commanded G instead of continuing past it into negative g - the same kinematic capture the load limiter uses, applied from above the target. Lower = a softer bound (0.5 allows twice the nose-down rate); 0 = off (pre-1.8.10 behaviour). Only in the G-command regime.");
			cKpG = dev.Bind("4. Gains", "GGain", 2.5f, "G command bandwidth (1/s): load error is converted to AoA error through the live lift slope.");
			cKiG = dev.Bind("4. Gains", "GIntegral", 1f, "G command integral gain (1/s^2), removes steady load error.");
			cFlaperonEff = dev.Bind("5. Airframe", "FlaperonEffectiveness", 1.5f, "Aerodynamic effectiveness multiplier on the flaperons and ailerons (scales their lifting area). Applied at spawn.");
			cStabEff = dev.Bind("5. Airframe", "StabilatorEffectiveness", 1.1f, "Aerodynamic effectiveness multiplier on the stabilators. Applied at spawn.");
			cActuator = dev.Bind("5. Airframe", "ActuatorSpeedScale", 1.428571f, "Servo speed multiplier for every control surface and the TVC nozzles (Aryx's servos run at 70 deg/s; 1.428571 = 100 deg/s).");
			cStabUp = dev.Bind("5. Airframe", "StabilatorRangeTEUp", 35f, "Stabilator travel, trailing edge up (deg). 0 = Aryx's original (30, symmetric).");
			cStabDn = dev.Bind("5. Airframe", "StabilatorRangeTEDown", 30f, "Stabilator travel, trailing edge down (deg). 0 = Aryx's original (30, symmetric).");
			cRudRange = dev.Bind("5. Airframe", "RudderRange", 35f, "Rudder travel each way (deg). 0 = Aryx's original (28).");
			cFlapRange = dev.Bind("5. Airframe", "FlaperonRange", 25f, "Travel each way for both flaperon pairs, outer (Aileron_*) and inner (Flap_*) (deg). 0 = Aryx's original (20 outer, 14 inner).");
			cTvcRange = dev.Bind("5. Airframe", "TvcPitchRange", 20f, "Pitch thrust-vectoring travel each way (deg, game maximum 20). 0 = Aryx's original (10).");
			cTvcSpeed = dev.Bind("5. Airframe", "TvcSlewRate", 75f, "TVC nozzle slew rate (deg/s). 0 = follow ActuatorSpeedScale like the control surfaces (70 x scale).");
			cLerxEff = dev.Bind("5. Airframe", "LERXEffectiveness", 1.6f, "Aerodynamic effectiveness multiplier on the LERX (lifting area). Forward lift: moves the neutral point forward (less static stability, less trim).");
			cCgShift = dev.Bind("5. Airframe", "CGShiftAft", 0.05f, "Moves the centre of mass aft by this many metres (mass moved from the forward fuselage to the aft fuselage, total mass unchanged). Main gear is ~0.3 m behind the stock CG: keep this small.");
			cFuelUse = dev.Bind("5. Airframe", "FuelConsumptionScale", 0.75f, "Fuel burn multiplier for every engine at every throttle setting, afterburner included (0.75 = 25 % less fuel used). All F-22Es.");
			cThrust = dev.Bind("5. Airframe", "ThrustScale", 1.08f, "Engine thrust multiplier (dry staticThrust and afterburner thrust alike, so max-AB total scales the same). Fuel flow per power setting is unchanged.");
			cDrag = dev.Bind("5. Airframe", "ParasiticDragScale", 0.885f, "Multiplier on every part's parasitic drag area (1 = stock). 0.885 = about -10 % total straight-line drag (parasitic is 85-93 % of it).");
			cDrAoA = dev.Bind("5. Airframe", "AoADragRelief", 0.25f, "1.8.38: fraction of the drag that comes from AoA (each part's airfoil drag above that airfoil's minimum) removed at every AoA. A pure scaling of the AoA-dependent drag, so the drag-vs-AoA curve keeps the game's shape with no bump or dip anywhere (a band limited to 5-27 deg would need hand-over zones that make the drag slope dip or hump). 0.25 = drag -20 % averaged over 5-27 deg AoA from this relief alone (about -11 % at 5, -18 % at 10, -22 % at 20, -23 % at 27; -4 % at 2, 0 at 0), on top of TransonicDragRelief; nothing changes at the minimum-drag AoA. Given back per part with the game's own drag model, as a force along each part's airflow. 0 = off. Ignored in the Stock PerformanceProfile.");
			cDrTrans = dev.Bind("5. Airframe", "TransonicDragRelief", 0.2f, "1.8.38: Mach drag relief. The slope of drag vs Mach is scaled from the game's (below Mach 0.2) down to 1 - 1.1 x this (above Mach 0.7) and held there, never coming back, so there is no dip or hump in the drag curve. With 0.2: total drag -6 % at Mach 0.5, -12 % at 0.7, -16 % at 0.9, -17 % at 1.0, -19 % at 1.2, -20 % at 1.5, -20.5 % at 1.8. 0 = off. Ignored in the Stock PerformanceProfile.");
			cAsymOn = dev.Bind("Asymmetric load", "AsymLoadAoALimit", defaultValue: true, "Cap the AoA when an asymmetric load (e.g. one heavy store on one wing) would use more of the roll/yaw control authority than AsymLateralReserve leaves free. The cap is the stick's full-travel AoA (the stick is remapped onto it). A load within the margin is simply trimmed out by the control loop (no cap). Not applied in MPO.");
			cAsymRollHead = dev.Bind("Asymmetric load", "AsymRollHeadroom", defaultValue: true, "The roll-rate and roll-acceleration limits count only the roll authority left in each direction after the measured asymmetric trim (asymmetric store, wing damage). The trim is learned from the measured non-control roll moment minus roll damping while the jet is not rolling (under 20 deg/s) and held during rolls; it only ever lowers the limit on the loaded side. With the left wing shot off it stops a full right roll at the AoA cap from saturating the stabs and rudders, building sideslip and departing. No effect on a symmetric jet. false = pre-1.8.17.");
			cAsymReserve = dev.Bind("Asymmetric load", "AsymLateralReserve", 0.35f, "Share of the roll/yaw control authority kept free for the pilot's roll and pedal commands (0..1). Used together with AsymRollReserveAccel / AsymYawReserveAccel: whichever asks for more sets the cap. Higher = lower cap, more roll/yaw left.");
			cAsymDead = dev.Bind("Asymmetric load", "AsymDeadbandAccel", 5f, "Asymmetric roll acceleration (deg/s^2) ignored before any cap is considered - the airframe is never perfectly symmetric even clean (off-centre gun and ammunition, engines never exactly matched). The yaw deadband is a quarter of this. A balanced loadout stays well inside it; one 300 kg store is far outside it.");
			cAsymRollAcc = dev.Bind("Asymmetric load", "AsymRollReserveAccel", 80f, "Roll acceleration (deg/s^2) that must stay available to the pilot after the asymmetric load is trimmed out, whatever AsymLateralReserve works out to. Post-stall a share of the remaining authority is almost nothing, so this absolute floor is what sets the cap up high and at low speed. Higher = lower cap.");
			cAsymYawAcc = dev.Bind("Asymmetric load", "AsymYawReserveAccel", 20f, "Same for yaw acceleration (deg/s^2).");
			cAsymFF = dev.Bind("Asymmetric load", "AsymFeedForward", 0f, "DEFAULT 0 (off) since 1.8.17. Hands the CHANGE in the modelled asymmetric moment straight to the allocator, washed out over AsymFeedForwardWashout seconds. Inside the INDI loop an added moment is not a one-off kick: the loop measures the acceleration it causes and settles with a steady roll-rate error of about FF/(I x KRoll) for as long as the term lasts, and while the load keeps changing (pulling, levelling off) it lasts the whole time. With a wing shot off it gave a 20-35 deg/s roll drift in pulls and level-offs and helped depart the jet. The loop already measures the asymmetric moment. 1 = pre-1.8.17.");
			cAsymFFTau = dev.Bind("Asymmetric load", "AsymFeedForwardWashout", 1f, "Time constant (s) over which the feed-forward above hands the load back to the control loop.");
			cAsymFFBeta = dev.Bind("Asymmetric load", "AsymFeedForwardMaxSideslip", 6f, "AsymFeedForward fades out over the 12 deg of sideslip above this and is off beyond it: the asymmetric moment it is built from is modelled at zero sideslip, so at large sideslip (falling leaf) it is not a measurement of anything.");
			cRevRoll = dev.Bind("3. Lateral-directional", "ReversedFlowRollSense", defaultValue: true, "Flying backwards (past 100 deg AoA, released below 80): the velocity-vector roll maps to body roll through cos(AoA), which flips sign there, so the stick would roll the jet the other way from the pilot's seat. This flips the roll command with it. Pitch and pedal are unaffected - the control loop already handles reversed flow on the surfaces themselves.");
			cAsymFFAoA = dev.Bind("Asymmetric load", "AsymFeedForwardMaxAoA", 40f, "AsymFeedForward fades out over the 15 deg of AoA above this and is off beyond it. Post-stall the lateral authority it would spend is what the manoeuvre needs.");
			cAsymRollTau = dev.Bind("Asymmetric load", "AsymRollTau", 0.5f, "The roll reserve also has to cover the roll rate the law allows at this speed, reached in this many seconds. At high IAS that is what sets the reserve (250 deg/s in 0.5 s = 500 deg/s^2), not AsymRollReserveAccel. 0 = off (fixed reserve only).");
			cAsymRelaxBoost = dev.Bind("Asymmetric load", "AsymCapRelaxBoost", 4f, "The cap relaxes faster the further the demand is below the limit: rate = AsymCapRelaxRate x (1 + this x margin). With the load gone the cap comes back at (1 + this) x the base rate. 0 = fixed rate.");
			cAsymBoost = dev.Bind("Asymmetric load", "AsymAxisPriorityBoost", 0f, "With more than half the lateral authority taken by an asymmetric load, the allocator's roll and yaw weight is raised by up to this factor (1 = up to double) so the surfaces hold the jet straight before they fine-tune pitch. DEFAULT 0 (off) since 1.7.11: it starves the pitch axis, which showed up as the nozzles running nose-down in rolling pulls.");
			cAsymFloor = dev.Bind("Asymmetric load", "AsymMinAoACap", 12f, "The asymmetric-load cap never goes below this positive AoA (deg).");
			cAsymFloorNeg = dev.Bind("Asymmetric load", "AsymMinAoACapNegative", 8f, "The asymmetric-load cap never goes closer to 0 than this on the negative side (deg, magnitude).");
			cAsymTighten = dev.Bind("Asymmetric load", "AsymCapTightenRate", 10f, "How fast the cap comes down when the asymmetry grows (deg/s).");
			cAsymRelax = dev.Bind("Asymmetric load", "AsymCapRelaxRate", 8f, "How fast the cap goes back up when the asymmetry shrinks (store released, fuel, speed) (deg/s).");
			cAsymRec = dev.Bind("Asymmetric load", "AsymCapRecoveryRate", 15f, "Above the cap (it just came down), AoA is brought back to it at up to this rate (deg/s). Not cancelled by full stick.");
			cAsymMinIas = dev.Bind("Asymmetric load", "AsymMinIAS", 35f, "Below this IAS (m/s) the cap is held at its last value instead of being recomputed (no aero authority to judge by).");
			cAsymCapDemand = dev.Bind("Asymmetric load", "AsymCapDemand", 1.08f, "1.8.45: lateral demand (load share of the authority left after the reserve) at which the asymmetric-load AoA cap sits (1.8.44 and before: 1.0). With a light mixed load (6.5 cm lateral CG offset) the demand rises ~0.025 per degree above the cap, so 1.08 lets ~3 deg more AoA; heavier loads, whose demand rises faster with AoA, gain less.");
			cAsymCapSmooth = dev.Bind("Asymmetric load", "AsymCapSmoothing", 1f, "1.8.45: time constant (s) with which the asymmetric-load cap may rise. Each 0.2 s sweep's cap goes through a median of the last three and then this low-pass; a lower cap is followed with 0.3 s. The raw cap moved 1-8 deg every sweep (4.4 Hz) with a 6.5 cm lateral offset at 35-40 deg AoA, which is what made full-aft stick twitch.");
			cDecelLook = dev.Bind("Asymmetric load", "DecelLookahead", 1f, "1.8.45: seconds of IAS decay projected ahead (filtered IAS rate, rapid decelerations only, see DecelStart/DecelFull). The asymmetric-load cap is judged, and the high-AoA roll/yaw rate limits are set, at the dynamic pressure of that predicted IAS, so they come down before the surfaces lose the authority. 0 = off.");
			cDecelStart = dev.Bind("Asymmetric load", "DecelStart", 6f, "1.8.45: IAS deceleration (m/s^2) below which nothing is anticipated. Between this and DecelFull the deceleration is weighted by the square of where it sits, so ordinary turning bleed (and most of a J-turn's tail) predicts almost nothing and a collapsing corkscrew predicts nearly all of it.");
			cDecelFull = dev.Bind("Asymmetric load", "DecelFull", 20f, "1.8.45: IAS deceleration (m/s^2) from which it is anticipated in full.");
			cHiAoABrake = dev.Bind("3. Lateral-directional", "HighAoABrakeFloor", true, "1.8.45: from 25 to 35 deg AoA up, the roll and yaw rate limits may not exceed what the stop-side authority (at the IAS predicted by DecelLookahead) brakes in the stop time of the reference manoeuvre, full pedal + full lateral stick at 65 deg AoA and 280 km/h (RollStopFactor for roll, YawStopFactor x the yaw boost at 280 km/h for yaw). Replaces there the fixed 30 deg/s roll and 20 x boost yaw floors, which outran the authority below ~65 m/s IAS. Costs yaw rate in slow post-stall yaw: bench, 65 deg AoA at 200 km/h 34 -> 23 deg/s, 300 km/h 37 -> 32. false = 1.8.44.");
			cCmdShape = dev.Bind("4. Gains", "CommandShaping", true, "1.9.0: AoA/G command pre-filter. The AoA and G commands are shaped before the loop: a small request (a few degrees or a fraction of a g from where the jet is) builds up gently, a large one runs as before. The shaping speed (1/s) grows with the step from ShapingAoAGainSmall to ShapingAoAGainLarge over ShapingAoASpan (G: ShapingG*), and small steps also ramp their onset over ShapingOnset. The same idea as the self-adjusting pre-filter gain of the T-50 control law (60 % gain below 1 g of error, 100 % above 3 g). false = 1.8.45.");
			cShapeASpan = dev.Bind("4. Gains", "ShapingAoASpan", 15f, "1.9.0: AoA step (deg) from which the AoA command is no longer slowed.");
			cShapeAKSmall = dev.Bind("4. Gains", "ShapingAoAGainSmall", 1.5f, "1.9.0: shaping speed (1/s) of the AoA command for a very small step. Lower = gentler fine tracking.");
			cShapeAKLarge = dev.Bind("4. Gains", "ShapingAoAGainLarge", 10f, "1.9.0: shaping speed (1/s) of the AoA command at and above ShapingAoASpan (fast enough to be transparent).");
			cShapeOnset = dev.Bind("4. Gains", "ShapingOnset", 0.25f, "1.9.0: onset time (s) of a very small AoA/G step (the command's rate builds up over this); fades to 0.03 s at the span.");
			cShapeGSpan = dev.Bind("4. Gains", "ShapingGSpan", 3f, "1.9.0: load step (g) from which the G command is no longer slowed.");
			cShapeGKSmall = dev.Bind("4. Gains", "ShapingGGainSmall", 1.5f, "1.9.0: shaping speed (1/s) of the G command for a very small step (onset and unload).");
			cShapeGKLarge = dev.Bind("4. Gains", "ShapingGGainLarge", 10f, "1.9.0: shaping speed (1/s) of the G command at and above ShapingGSpan.");
			cRateBlend = dev.Bind("2. Envelope", "HighAoARateBlend", true, "1.9.1: AoA command hands over to pitch-rate command past RateBlendAoA. The stick scale is fixed: RateBlendAoA at RateBlendStick (36 deg at 0.5 = 72 deg per full stick); the threshold's stick point moves with the threshold (24 deg -> 0.333). Up to that point the stick is AoA command; past it, still AoA command, on to the AoA limit at full stick (full stick goes through the threshold at the rate budget). Holding the threshold's point holds the threshold AoA. When the jet crosses the threshold with the stick past its point, the stick becomes a pitch-rate command starting from the pitch rate at the crossing, up to the budget at full stick; a full pull or a release switches it to 0..full stick = 0..budget, and a release holds zero rate. Easing below the point stays rate command until the jet is back at the threshold. If the jet comes back to the threshold with the stick still past the point, that stick position is the threshold: easing from it holds the live threshold (soft knee) until the stick meets the threshold's own point on the line. false = AoA command to the limit (1.8.45).");
			cRateBlendStick = dev.Bind("2. Envelope", "RateBlendStick", 0.5f, "1.9.1: stick position of RateBlendAoA; sets the AoA per stick below the threshold (36 / 0.5 = 72 deg per full stick). It stays fixed when the threshold moves.");
			cRateBlendAoA = dev.Bind("2. Envelope", "RateBlendAoA", 36f, "1.9.0: AoA (deg) where the AoA command hands over to pitch-rate command when the wings still lift at least RateBlendLiftG times the weight.");
			cRateBlendAoAMin = dev.Bind("2. Envelope", "RateBlendAoAMin", 25f, "1.9.0: the handover AoA when the wings can lift only the weight (1 g, thrust not counted); in between it is interpolated by the lift-to-weight ratio (dynamic pressure x wing lift / mass). Slower = lower threshold.");
			cRateBlendLiftG = dev.Bind("2. Envelope", "RateBlendLiftG", 2f, "1.9.0: wing lift-to-weight ratio (g, thrust not counted) from which the full RateBlendAoA applies. ~330 km/h clean at combat weight.");
			cRateBlendBand = dev.Bind("2. Envelope", "RateBlendBand", 4f, "1.9.0: AoA band (deg) below the threshold over which rate command and AoA command are blended once the rate command is engaged (pitch rate stays continuous across it).");
			cRateBlendNeg = dev.Bind("2. Envelope", "HighAoARateBlendNegative", true, "1.9.7: the same AoA -> pitch-rate hand-over on the negative side, mirrored: RateBlendAoANegative at -RateBlendStick (-36 deg at -0.5 = 72 deg per full push, the same scale as the positive side), full push = the negative AoA limit, rate command (nose-down) past the threshold, floating hold point. RateBlendStick, RateBlendLiftG and RateBlendBand are shared. false = linear AoA command to the negative limit (1.9.6).");
			cRateBlendAoANeg = dev.Bind("2. Envelope", "RateBlendAoANegative", 36f, "1.9.7: negative hand-over AoA magnitude (deg) while the wings can still push at least RateBlendLiftG times the weight nose-down (negative lift).");
			cRateBlendAoAMinNeg = dev.Bind("2. Envelope", "RateBlendAoAMinNegative", 25f, "1.9.7: negative hand-over AoA magnitude when the wings' negative lift is only the weight; interpolated by the negative lift-to-weight ratio in between.");
			cAuthCap = dev.Bind("2. Envelope", "LowThrustAoACap", true, "1.9.0: the AoA limit is held below the AoA where the nose-down authority you have now runs out: stabilators full nose-down, flaperons at their trim, plus the nozzles at the present thrust (none above the thrust-vectoring cut-off, 450 m/s true airspeed - the high-altitude supersonic case where the nozzles freeze). Bench, 350 km/h: zero thrust 23 deg, ~0.1 T/W 26 deg, 30 % throttle 34 deg, full afterburner no cap; a full pull then full push recovers in every case (1.8.45 departed to 73-90 deg at the first two). The stick is remapped onto the capped envelope like the asymmetric-load cap. false = 1.8.45.");
			cAuthCapMargin = dev.Bind("2. Envelope", "LowThrustCapMargin", 8f, "1.9.0: nose-down pitch acceleration (deg/s^2) that must be left at the cap. Higher = lower cap.");
			cAuthCapFloor = dev.Bind("2. Envelope", "LowThrustCapFloor", 20f, "1.9.0: the low-thrust AoA cap never goes below this (deg).");
			cHybridIndi = dev.Bind("4. Gains", "HybridIndi", 1f, "1.9.0: pitch-loop lag compensation. The incremental (INDI) pitch loop estimates the airframe's own moment from filtered sensors; at high dynamic pressure that moment moves fast with AoA and pitch rate and the filter lag made the pitch rate overshoot (bench, 2 g command at 1300 km/h: 2.8 g peak then 1.8 g, a slow G oscillation). The lag is now predicted from the airframe model (moment slope with AoA and pitch rate x the filter lag). 0 = off (1.8.45).");
			cYawRefKmh = dev.Bind("3. Lateral-directional", "YawBrakeRefIAS", 270f, "1.9.2: IAS (km/h) of the reference manoeuvre for the high-AoA yaw braking floor (HighAoABrakeFloor): full pedal + full lateral stick at max AoA. The yaw-rate limit may not exceed what the yaw authority brakes in YawStopFactor x the low-IAS yaw boost at this IAS (1.8.45: 280).");
			cYawMargStart = dev.Bind("3. Lateral-directional", "YawStopTimeStartIAS", 210f, "1.9.3: below this IAS (km/h) the allowed yaw stop time grows smoothly (smoothstep) to YawStopTimeLowIAS x at YawStopTimeFullIAS; flat above and below.");
			cYawMargFull = dev.Bind("3. Lateral-directional", "YawStopTimeFullIAS", 150f, "1.9.3: IAS (km/h) at and below which the full YawStopTimeLowIAS applies.");
			cYawMargLow = dev.Bind("3. Lateral-directional", "YawStopTimeLowIAS", 1.5f, "1.9.3: allowed yaw stop time at low IAS relative to the reference: 1.5 = 50 % more yaw rate for the same yaw authority (slower braking). 1 = flat. (1.9.2's YawBrakeMargin* keys worked the other way and are no longer read.)");
			cYawMidF = dev.Bind("3. Lateral-directional", "YawStopTimeMidFactor", 0.8f, "1.9.4: high-AoA yaw stop time (so yaw-rate limit) factor between YawStopTimeMidLoIAS and YawStopTimeMidHiIAS: 0.8 = 20 % less yaw rate for the same authority, faster braking. Smoothstep ramps of YawStopTimeMidRamp km/h outside the band. 1 = off.");
			cYawMidLo = dev.Bind("3. Lateral-directional", "YawStopTimeMidLoIAS", 290f, "1.9.4: lower edge (km/h) of the mid-IAS yaw band (full factor from here up).");
			cYawMidHi = dev.Bind("3. Lateral-directional", "YawStopTimeMidHiIAS", 330f, "1.9.4: upper edge (km/h) of the mid-IAS yaw band (full factor up to here).");
			cYawMidRamp = dev.Bind("3. Lateral-directional", "YawStopTimeMidRamp", 30f, "1.9.4: width (km/h) of the smooth ramps outside the mid-IAS yaw band (260-290 and 330-360 by default).");
			DevUnlocked = devUnlocked;
			MigrateOldConfig(cfg, dev, devUnlocked && devFresh);
		}

		public static bool DevUnlocked { get; private set; }

		// 1.8.44: the config was split into the player file (this plugin's) and the dev/debug file. Keys left in the player
		// file from older builds are orphans: the ones that moved section inside the player file (AI toggles) and, when the
		// dev file is being created now, the dev settings are carried over by key name once; then the orphans are dropped.
		private static void MigrateOldConfig(ConfigFile cfg, ConfigFile dev, bool toDev)
		{
			try
			{
				var prop = typeof(ConfigFile).GetProperty("OrphanedEntries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
				if (!(prop?.GetValue(cfg) is Dictionary<ConfigDefinition, string> orph) || orph.Count == 0)
				{
					return;
				}
				var byKey = new Dictionary<string, string>();
				foreach (KeyValuePair<ConfigDefinition, string> o in orph)
				{
					if (o.Key.Section.StartsWith("Countermeasures (all") || !byKey.ContainsKey(o.Key.Key))
					{
						byKey[o.Key.Key] = o.Value;
					}
				}
				int n = 0;
				void Carry(ConfigFile f, bool skipStockCm)
				{
					foreach (ConfigDefinition def in new List<ConfigDefinition>(f.Keys))
					{
						if (skipStockCm && def.Section.StartsWith("3. Countermeasures"))
						{
							continue;
						}
						if (!byKey.TryGetValue(def.Key, out string v))
						{
							continue;
						}
						ConfigEntryBase e = f[def];
						try
						{
							e.BoxedValue = TomlTypeConverter.ConvertToValue(v, e.SettingType);
							n++;
						}
						catch
						{
						}
					}
				}
				Carry(cfg, skipStockCm: true);
				if (toDev)
				{
					Carry(dev, skipStockCm: false);
					dev.Save();
				}
				orph.Clear();
				cfg.Save();
				Log.LogInfo($"F-22E FCS: old config keys migrated ({n} values carried over{(toDev ? ", dev file created" : "")}, leftovers removed).");
			}
			catch (Exception ex)
			{
				Log.LogWarning("F-22E FCS config migration: " + ex.Message);
			}
		}

		public static void ApplyPatches(Harmony h)
		{
			TryPatch(h, "ControlsFilter.Filter", delegate
			{
				h.Patch(AccessTools.Method(typeof(ControlsFilter), "Filter"), new HarmonyMethod(typeof(Hooks), "FilterPrefix"));
			});
			TryPatch(h, "AutopilotPlane.AutoAim", delegate
			{
				h.Patch(AccessTools.Method(typeof(AutopilotPlane), "AutoAim", new Type[9]
				{
					typeof(GlobalPosition),
					typeof(bool),
					typeof(bool),
					typeof(bool),
					typeof(float),
					typeof(float),
					typeof(bool),
					typeof(float),
					typeof(Vector3)
				}), new HarmonyMethod(typeof(Hooks), "AutoAimPrefix"), new HarmonyMethod(typeof(Hooks), "AutoAimPostfix"));
			});
			TryPatch(h, "ControlSurface.UpdateJobFields", delegate
			{
				h.Patch(AccessTools.Method(typeof(ControlSurface), "UpdateJobFields"), null, new HarmonyMethod(typeof(Hooks), "SurfacePostfix"));
			});
			TryPatch(h, "PowerSupply.ModifyCapacitance", delegate
			{
				h.Patch(AccessTools.Method(typeof(PowerSupply), "ModifyCapacitance"), new HarmonyMethod(typeof(Countermeasures), "ModifyCapacitancePrefix"));
			});
			TryPatch(h, "Aircraft.UseFuel", delegate
			{
				h.Patch(AccessTools.Method(typeof(Aircraft), "UseFuel", new Type[1] { typeof(float) }), new HarmonyMethod(typeof(Countermeasures), "UseFuelPrefix"));
			});
			TryPatch(h, "HighLiftDevice.FixedUpdate", delegate
			{
				h.Patch(AccessTools.Method(typeof(HighLiftDevice), "FixedUpdate"), new HarmonyMethod(typeof(Hooks), "FlapDevicePrefix"));
			});
			if (FlapSystem.ResponderType != null)
			{
				TryPatch(h, "AryxAlphaResponder.FixedUpdate", delegate
				{
					h.Patch(AccessTools.Method(FlapSystem.ResponderType, "FixedUpdate"), new HarmonyMethod(typeof(Hooks), "FlapDevicePrefix"));
				});
			}
			else
			{
				Log.LogWarning("AryxAlphaResponder type not found (original F-22E mod not loaded?) - flap takeover limited to the high-lift devices.");
			}
			if (Hooks.VentType != null)
			{
				TryPatch(h, "Aryx_OverpressureVentController.CalculateDeployment", delegate
				{
					h.Patch(AccessTools.Method(Hooks.VentType, "CalculateDeployment"), null, new HarmonyMethod(typeof(Hooks), "VentPostfix"));
				});
			}
			else
			{
				Log.LogInfo("Aryx_OverpressureVentController not found - no vent doors to take over.");
			}
			TryPatch(h, "LandingGear.FixedUpdate", delegate
			{
				h.Patch(AccessTools.Method(typeof(LandingGear), "FixedUpdate"), new HarmonyMethod(typeof(Hooks), "GearPrefix"), new HarmonyMethod(typeof(Hooks), "GearPostfix"));
			});
			TryPatch(h, "Turbofan.FixedUpdate", delegate
			{
				h.Patch(AccessTools.Method(typeof(Turbofan), "FixedUpdate"), new HarmonyMethod(typeof(Hooks), "TurbofanPrefix"), null, new HarmonyMethod(typeof(Hooks), "TurbofanTranspiler"));
			});
			Log.LogInfo($"F-22E FCS {FcsStandalonePlugin.Version} ({(StockPerformance ? "stock" : "buffed")} performance{(DevUnlocked ? ", dev/debug settings unlocked" : "")}) ready (AoA +{AoAPos}/{AoANeg}, G +{GPos}/{GNeg}, MPO +{MpoGPos}/{MpoGNeg}, " + string.Format("q<= {0}, p<= {1}, r<= {2}; asym-load AoA cap {3}; flaperon x{4}, stab x{5}, actuators x{6}, thrust x{7}, parasitic drag x{8}).", MaxPitchRate, MaxRollRate, MaxYawRate, AsymLimiter ? ("on, reserve " + AsymReserve.ToString("0.00")) : "off", FlaperonEffectiveness, StabilatorEffectiveness, ActuatorScale, ThrustScale, DragScale));
		}

		private static void TryPatch(Harmony h, string what, Action a)
		{
			try
			{
				a();
				Log.LogInfo("Installed hook: " + what);
			}
			catch (Exception ex)
			{
				Log.LogError("Failed to install hook " + what + ": " + ex);
			}
		}
	}
}
