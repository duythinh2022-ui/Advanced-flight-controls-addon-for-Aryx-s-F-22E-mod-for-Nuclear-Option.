using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal sealed class FcsController
	{
		public readonly Aircraft ac;

		public readonly ControlInputs inputs;

		public readonly List<SurfaceEffector> surfaces = new List<SurfaceEffector>();

		public readonly TvcEffector tvc;

		public readonly FlapSystem flaps;

		private readonly List<Effector> effs = new List<Effector>();

		private readonly Allocator alloc = new Allocator();

		private readonly List<LandingGear> gears = new List<LandingGear>();

		private readonly List<Rigidbody> bodies = new List<Rigidbody>();

		private readonly List<AeroPart> aeroParts = new List<AeroPart>();

		private readonly Dictionary<AeroPart, AirfoilTable> foils = new Dictionary<AeroPart, AirfoilTable>();

		public bool Engaged;

		public bool Failed;

		public FcsMode Mode;

		private float mass = 20000f;

		private Vector3 cg;

		private readonly float[,] Ia = new float[3, 3];

		private readonly float[,] IaInv = new float[3, 3];

		private float massTimer = 99f;

		private float nAlphaTimer = 99f;

		private Vector3 omega;

		private Vector3 omegaPrev;

		private Lpf3 omegaDotF;

		private Lpf3 mNowF;

		private Lpf3 accF;

		private Vector3 velPrev;

		private bool havePrev;

		private Lpf alphaDotF;

		private Lpf aUpF;

		private Lpf aDnF;

		private Lpf qTdotF;

		private float alphaPrev;

		public float alphaU;

		public bool protHold;

		private float protHoldSide;

		public float alpha;

		public float beta;

		public float tas;

		public float ias;

		public float nz;

		public float omegaV;

		public float eLy;

		public float phi;

		public float theta;

		public float alphaDot;

		public float nAlpha = 0.1f;

		public float tw;

		public float nMaxAero = 9f;

		// 1.9.0: wing lift-to-weight ratio at the lift peak, thrust not counted
		public float nLiftMax = 9f;

		// 1.9.0 command shaping (AoA/G pre-filter)
		public float alphaCmdRaw;

		public float nCmdRaw = 1f;

		private float aRefV;

		private float nRefV;

		private bool shapeInit;

		// 1.9.0 AoA -> pitch-rate hand-over
		public float rateBlendAoA = 36f;

		// 1.9.7 negative side (magnitude), which side the rate command / floating hold belongs to, wing negative-lift capacity
		public float rateBlendAoANeg = 36f;

		private int hiSide = 1;

		private int floatSide = 1;

		public float nLiftMin = 9f;

		private bool rbInit;

		public bool hiRate;

		public float rateW;

		public float zoneX;

		private bool hiAnchor;

		public bool floatOn;

		public float floatS;

		internal static float FloatKnee = 2f;

		private float hiAS;

		private float hiAQ;

		// 1.9.0 low-thrust (nose-down authority) AoA cap
		public float authCap = 999f;

		public float authCapEff = 999f;

		private bool authInit;

		internal static float AuthCapLead = 1f;

		internal static float ReleaseTauG = 0.3f;

		internal static float AuthCapTauUp = 1f;

		internal static float AuthCapTauDown = 0.3f;

		internal static float AuthCapRise = 4f;

		internal static float AuthCapRiseAtCap = 1f;

		internal static float AuthCapThrustStep = 1f;   // deg/s^2 of nozzle nose-down authority gained between sweeps

		internal static float AuthCapFastHold = 1f;     // s the fast rise stays open after the last such gain

		private float authTvPrev;

		private float authFastT;

		public float alphaAtNMax = 30f;

		public float gAvail = 9f;

		public float gInt;

		public float vDot;

		public float alphaDotHoldG;

		private Lpf vDotF;

		public float aUp = 60f;

		public float aDn = 60f;

		internal static float RollLoadLead = 0.2f;

		internal static float RollStickRate = 10f;

		internal static float PreLag = 0.01f;

		internal static bool LegacyLag;

		internal static float RollJerkServo = 1f;

		internal static float RollJerkTau = 0.18f;

		internal static bool BrakeUseAll = true;

		internal static float RollEndJerk = 0.35f;

		internal static float BrakeYawCap = 1.3f;

		internal static float RollEntryCoord = 0.5f;

		internal static float AsymRollMax = 120f;

		internal static float AsymRollU0 = 0.1f;

		internal static bool AsymRollHardOnly = true;

		internal static float EndJerkHi = 2f;

		internal static float CoupGateA0 = 12f;

		internal static float CoupHeldGate = 1f;

		internal static float CoupHeldDy0 = 0.04f;

		internal static float CoupHeldDySpan = 0.08f;

		internal static float AsymPitchPrio = 1f;

		internal static float AsymPitchPrioLead = 3f;

		internal static float AsymStabAntiCost = 0f;

		internal static float AsymPitchCapLead = 2f;

		internal static float AsymPitchCapFloor = 10f;

		public float asymCapPitch = 999f;


		internal static float EndJerkLp0 = 1.5f;

		internal static float EndJerkLp1 = 2.5f;

		public float lpDbg;

		internal static float AIRollDirectAccel = 3000f;

		public bool aiGunTrack;

		public float aiRollFade = 1f;

		private float rollSlew = 100f;

		public static bool DebugYaw;

		public float dbgWVe;

		public float dbgYawRud;

		public float dbgYawWing;

		public float dbgYawStab;

		public float dbgYawTvc;

		public float dbgTargetZ;

		public float dbgMfZ;

		public float dbgTgtY;

		public float dbgMfY;

		public float aRoll = 300f;

		public float aYaw = 60f;

		private Lpf aRollF;

		private Lpf aYawF;

		private Lpf aRollWF;

		public float aRollWing = 200f;

		public float pS;

		private RateTraj pTraj;

		private RateTraj qTraj;

		private Lpf rSF;

		public float rudderAuth = 1f;

		// 1.8.35 sideslip rudder: signed yaw-command travel allowed beyond the normal rudder fade (deg), and the pedal
		// sideslip command it corrects toward (from the previous lateral-law pass)
		public float rudSideDbg;

		private float betaPedalCmd;

		private float rudSideF;

		// 1.8.36 ground steering
		public bool gsActive;

		public float gsNwsDeg;

		public float gsRCmd;

		public float gsRud;

		public bool gsHold;

		public float gsLineY;

		public float gsChiRef;

		private Vector3 gsLineP;

		private float gsInt;

		private float gsRudInt;

		private float gsBrakeT;

		private float gsLead = -1f;

		public float gsChiAir;

		private float gsSinceContact = 99f;

		public float gsAirT;

		public float aInt;

		public float yawSat;

		private Lpf yawSatF;

		private Lpf guardF;

		public float stabPitchFrac = 1f;

		public float tvcAnchor;

		public float tvcBudgetGate;

		private float tvcBudgetLat;

		private bool tvcAnchorInit;

		private float pCmdPrev;

		private float rCmdPrev;

		private Lpf pDotF;

		private Lpf rDotF;

		public float pFull = 300f;

		public float pStockRef;

		public float pMaxDbg;

		public float rMaxDbg;

		public float rollCtrlHalf = 100000f;

		public float guardDbg = 1f;

		public float pSmaxDbg;

		public float pSTgtDbg;

		public float latAccDbg;

		public float latJerkDbg;

		public float rSDbg;

		public bool revFlow;

		private bool revFlowLogged;

		public float sp;

		public float sr;

		public float sy;

		private float spF;

		private float srF;

		private float syF;

		public float qB;

		public float qRaw;

		public float qCmd;

		public float qT;

		public float qTprev;

		public float betaG;

		public float alphaCmd;

		public float nCmd;

		public bool unload;

		public bool prot;

		public float qMaxDbg;

		public float qMinDbg;

		public float gUnderDbg;

		public float pCmd;

		public float rCmd;

		public float pDotFF;

		public float rDotFF;

		public Vector3 omegaDotDes;

		public Vector3 mTarget;

		public float tvcPitchInput;

		private int groundFrames;

		public bool rotating;

		public bool mainsOnGround;

		public float groundBrake;

		public float airBrake;

		public bool onGround;

		private float[] brakeBias;

		private float gbLogTimer;

		private float noseDownTime;

		private float noseUpTime;

		private float airTime;

		private LandingGear noseGearCached;

		private bool noseGearSearched;

		private FcsMode lastLoggedMode;

		private Telemetry telemetry;

		private float time;

		private static readonly float[] envAlphas = new float[25]
		{
			-1f, 0f, 1f, 5f, 10f, 15f, 20f, 25f, 30f, 35f,
			40f, 45f, 50f, 55f, 60f, 65f, -5f, -10f, -15f, -20f,
			-25f, -30f, -35f, -40f, -45f
		};

		private const int EnvNegStart = 16; // 1.9.7: negative-lift points for the negative hand-over threshold

		private readonly float[] envLift = new float[25];

		private static readonly float[] asymAlphas = BuildAsymAlphas();

		private readonly float[] asymU = new float[22];

		private readonly float[] asymAr = new float[22];

		private readonly float[] asymAy = new float[22];

		private readonly float[] asymCr = new float[22];

		private readonly float[] asymCy = new float[22];

		private readonly float[] asymNetDn = new float[22];

		// 1.9.0: nose-down authority incl. the flaperons at their present deflection (asymNetDn leaves their lift out)
		private readonly float[] authNetDn = new float[22];

		private readonly float[] asymNetUp = new float[22];

		public float aBrakeTgt;

		internal static int CaptureMode = 1;



		private float asymTimer = 99f;

		private bool asymValid;

		public float asymCapPos = 999f;

		public float asymCapNeg = -999f;

		public float asymCapPosEff = 999f;

		public float asymCapNegEff = -999f;

		public float asymDy;

		public float asymArNow;

		public float asymAyNow;

		public float asymUNow;

		public float asymCrNow;

		public float asymCyNow;

		public Vector3 asymMNow;

		private Lpf3 asymMF;

		public Vector3 asymFF;

		public float asymFFr;

		public float asymFFy;

		public float asymFFGate;

		public float rollPitchFF;

		private float tvcPin;

		private bool tvcPinned;

		private readonly float[] pinLo = new float[16];

		private readonly float[] pinHi = new float[16];

		private readonly float[] symSave = new float[16];

		public float dMeasR;

		public float dMeasY;

		private Lpf dMeasRF;

		private Lpf dTrimRF;

		public float dTrimR;

		public float aRollPos = 300f;

		public float aRollNeg = 300f;

		internal static bool RollTrimHeadroom = true;

		private Lpf dMeasYF;

		private float asymLogTimer = 99f;

		private float asymLoggedCap = 999f;

		private float asymLoggedCapN = -999f;

		private readonly HashSet<AeroPart> surfaceParts = new HashSet<AeroPart>();

		private int asymK = -1;

		private bool asymCycleDone;

		private bool asymFirstLogged;

		internal static int AsymPointsPerStep = 3;

		private Vector3 asymMT;

		private float[] asymDsym;

		public float Lp = -100000f;

		private const float KCapture = 3f;

		internal static float YawBoostFixed = -1f;

		internal static bool RudderRevBack = true;

		internal static float YawFFAoA0 = 15f;

		internal static float YawFFAoA1 = 28f;

		internal static bool ReversalFloor = true;

		internal static float RevFloorK = 1f;

		internal static bool RevConst = true;

		internal static bool RevCarry = true;

		private float revA;

		private float revTarget;

		internal static float WashK = -1f;

		internal static float WashTau = -1f;

		internal static float WashUtilHi = 0.75f;

		internal static bool GUnderAboveOnly = true;

		private float washApplied;

		internal static float MLeadTau = 0.04f;

		internal static float IndiTau = 0.04f;

		// 1.9.0 hybrid INDI (pitch): the incremental law takes the airframe's own moment from filtered measurements, so
		// it lags the true one by the filter (IndiTau). At high dynamic pressure the non-control-surface moment moves fast
		// with AoA and pitch rate (the fuselage/LERX nose-up grows with AoA), and that lag let the pitch rate overshoot its
		// command by ~2 deg/s for 0.3 s at 1300 km/h - ~1.4 g at that speed, the G-command overshoot/undershoot cycle.
		// The lag is predicted from the airframe model: dM = dM/dAoA x (AoA - AoA filtered) + dM/dq x (q - q filtered),
		// slopes of the non-surface parts' moment evaluated every 0.1 s at the present flow.
		public float mAlphaO;

		public float mQO;

		public float hybridDM;

		private Lpf alphaIndiF;

		private Lpf qIndiF;

		private float mPrevY;

		private bool mLeadInit;

		private float mDotYF;

		public float mLeadGate;

		public float mLeadAgree = 1f;

		internal static bool PairedTrim = true;

		public float latReserve;

		public bool isAI;

		public float aiGLimit = 9.5f;

		public float aiCornerSpeed = 140f;

		private bool telemetryOpened;

		private float aiLogT;

		private float aiLogAMax;

		private float aiLogAMin;

		private float aiLogGMax;

		private float aiLogGMin;

		private float aiLogIasMin = 9999f;

		private float aiLogIasMax;

		private float aiLogProtT;

		private int aiLogN;

		private float aiLogP2;

		private int aiLogRev;

		private int aiLogRevSign;

		private float aiLogGunT;

		private float aiLogFadeT;

		private void AILogStep(float dt)
		{
			if (aiLogN == 0)
			{
				aiLogAMax = (aiLogAMin = alpha);
				aiLogGMax = (aiLogGMin = nz);
			}
			aiLogN++;
			aiLogT += dt;
			aiLogAMax = Mathf.Max(aiLogAMax, alpha);
			aiLogAMin = Mathf.Min(aiLogAMin, alpha);
			aiLogGMax = Mathf.Max(aiLogGMax, nz);
			aiLogGMin = Mathf.Min(aiLogGMin, nz);
			aiLogIasMin = Mathf.Min(aiLogIasMin, ias * 3.6f);
			aiLogIasMax = Mathf.Max(aiLogIasMax, ias * 3.6f);
			if (prot)
			{
				aiLogProtT += dt;
			}
			float pDeg = omega.x * 57.29578f;
			aiLogP2 += pDeg * pDeg * dt;
			int num = ((pDeg > 15f) ? 1 : ((pDeg < -15f) ? (-1) : 0));
			if (num != 0)
			{
				if (aiLogRevSign != 0 && num != aiLogRevSign)
				{
					aiLogRev++;
				}
				aiLogRevSign = num;
			}
			if (aiGunTrack)
			{
				aiLogGunT += dt;
				if (aiRollFade < 0.99f)
				{
					aiLogFadeT += dt;
				}
			}
			if (aiLogT >= 20f)
			{
				Plugin.Log.LogInfo($"F-22E FCS AI jet #{((UnityEngine.Object)(object)ac).GetInstanceID()} last {aiLogT:0}s: mode {Mode}, IAS {aiLogIasMin:0}-{aiLogIasMax:0} km/h, AoA {aiLogAMin:0.0}..{aiLogAMax:0.0} deg, g {aiLogGMin:0.0}..{aiLogGMax:0.0}, AoA protection active {aiLogProtT:0.0}s, roll rate rms {Mathf.Sqrt(aiLogP2 / Mathf.Max(aiLogT, 0.01f)):0} deg/s with {aiLogRev} left/right reversals (>15 deg/s), gun-tracking a moving target {aiLogGunT:0.0}s (roll faded {aiLogFadeT:0.0}s).");
				aiLogT = 0f;
				aiLogN = 0;
				aiLogProtT = 0f;
				aiLogP2 = 0f;
				aiLogRev = 0;
				aiLogGunT = 0f;
				aiLogFadeT = 0f;
				aiLogIasMin = 9999f;
				aiLogIasMax = 0f;
			}
		}

		public int stabBranch;

		public float pitchNeed;

		internal static float CoupRollFloor = 5f;

		public float coupRollLim;

		private bool branchGateOn;

		internal static float BranchFailTime = 0.25f;

		internal static float BranchCoolTime = 1f;

		internal static float BranchCoolTimeStop = 0.25f;

		internal static float LatTrimGain = 100f;

		public float aUpPitch;

		public float aDnPitch;


		internal static bool LeadAgreeGate = true;

		public float pitchPrio = 1f;

		public bool stabRateBound;

		private float tvcTrimRef;

		private float tvcSlow;

		private float[] cmdSave = new float[64];

		public bool SplitDeploy
		{
			get
			{
				if (Plugin.AirbrakeSplit && airBrake > 0.01f)
				{
					return !onGround;
				}
				return false;
			}
		}

		private LandingGear NoseGear()
		{
			if (noseGearSearched)
			{
				return noseGearCached;
			}
			noseGearSearched = true;
			LandingGear landingGear = null;
			float num = float.MinValue;
			float num2 = float.MinValue;
			int num3 = 0;
			foreach (LandingGear gear in gears)
			{
				if (!(gear == null))
				{
					num3++;
					float num4 = Vector3.Dot(gear.transform.position - ((ac.rb != null) ? ac.rb.worldCenterOfMass : ((Component)(object)ac).transform.position), ((Component)(object)ac).transform.forward);
					if (num4 > num)
					{
						num2 = num;
						num = num4;
						landingGear = gear;
					}
					else if (num4 > num2)
					{
						num2 = num4;
					}
				}
			}
			noseGearCached = ((num3 >= 2 && num - num2 > 1.5f) ? landingGear : null);
			Plugin.Log.LogInfo((noseGearCached != null) ? $"F-22E FCS: nose gear '{noseGearCached.name}' ({num - num2:F1} m ahead of the next leg), {num3} gear legs." : $"F-22E FCS: no distinct nose gear among {num3} gear legs; rotation law disabled.");
			return noseGearCached;
		}

		public FcsController(Aircraft aircraft)
		{
			ac = aircraft;
			inputs = aircraft.GetInputs();
			AircraftParameters aircraftParameters = aircraft.GetAircraftParameters();
			foreach (ControlSurface item in Acc.Collect<ControlSurface>(aircraft))
			{
				AeroPart aeroPart = Acc.CS_attached(item) as AeroPart;
				if (!(aeroPart == null))
				{
					SurfaceKind kind = Classify(item);
					surfaces.Add(new SurfaceEffector(item, aeroPart, kind, FoilFor(aeroPart, aircraftParameters)));
				}
			}
			if (surfaces.Count < 6)
			{
				throw new InvalidOperationException("F-22E FCS: expected 8 control surfaces, found " + surfaces.Count);
			}
			foreach (SurfaceEffector surface in surfaces)
			{
				if (surface.kind == SurfaceKind.Stabilator)
				{
					surface.mu = 0.0002f;
					surface.linearPitch = true;
				}
				else
				{
					surface.noPitch = true;
				}
				surface.rollGroup = ((surface.kind == SurfaceKind.Stabilator) ? 1 : ((surface.kind == SurfaceKind.Aileron) ? 2 : ((surface.kind == SurfaceKind.Flaperon) ? 3 : 0)));
				surface.pitchGroup = ((surface.kind == SurfaceKind.Stabilator) ? 1 : 0);
				effs.Add(surface);
			}
			foreach (SurfaceEffector l in surfaces)
			{
				if (!l.name.EndsWith("_L"))
				{
					continue;
				}
				SurfaceEffector surfaceEffector = surfaces.Find((SurfaceEffector o) => o.kind == l.kind && o.name == l.name.Substring(0, l.name.Length - 2) + "_R");
				if (surfaceEffector == null)
				{
					continue;
				}
				bool num = l.kind == SurfaceKind.Rudder;
				float num2 = (num ? Mathf.Sign(l.origYawRange) : Mathf.Sign(l.origRollRange));
				float num3 = (num ? Mathf.Sign(surfaceEffector.origYawRange) : Mathf.Sign(surfaceEffector.origRollRange));
				if (num2 != 0f && num3 != 0f && num2 != num3)
				{
					l.partner = surfaceEffector;
					l.pairSign = num2;
					surfaceEffector.pairSign = num3;
					if (l.kind == SurfaceKind.Stabilator)
					{
						l.symCost = Plugin.StabTrimCost;
						l.antiCost = 0.01f;
					}
					else
					{
						l.symCost = 2f;
						l.antiCost = 0f;
					}
				}
			}
			tvc = new TvcEffector(Acc.Collect<Turbofan>(aircraft));
			effs.Insert(0, tvc);
			tvc.pitchGroup = 2;
			gears.AddRange(Acc.Collect<LandingGear>(aircraft));
			foreach (UnitPart item2 in aircraft.partLookup)
			{
				if (item2 is AeroPart aeroPart2)
				{
					aeroParts.Add(aeroPart2);
					if (!foils.ContainsKey(aeroPart2))
					{
						foils[aeroPart2] = FoilFor(aeroPart2, aircraftParameters);
					}
				}
			}
			RebuildBodies();
			flaps = new FlapSystem(aircraft, surfaces);
			Plugin.Log.LogInfo(string.Format("F-22E FCS attached: {0} surfaces [{1}], ", surfaces.Count, string.Join(", ", surfaces.ConvertAll((SurfaceEffector s) => s.name + ":" + s.kind.ToString() + ((s.min == -s.max) ? (" ±" + s.max.ToString("0")) : (" " + s.min.ToString("0") + "/+" + s.max.ToString("0")))))) + $"TVC engines {tvc.engines.Count} (±{tvc.max:0} deg pitch only), {bodies.Count} rigidbodies, {flaps.Describe()}.");
		}

		private static SurfaceKind Classify(ControlSurface cs)
		{
			string name = cs.gameObject.name;
			if (Mathf.Abs(Acc.CS_pitchRange(cs)) > 0.01f || name.Contains("Elevator") || name.Contains("Stab"))
			{
				return SurfaceKind.Stabilator;
			}
			if (name.Contains("Rudder"))
			{
				return SurfaceKind.Rudder;
			}
			if (name.Contains("Flap"))
			{
				return SurfaceKind.Flaperon;
			}
			if (name.Contains("Aileron"))
			{
				return SurfaceKind.Aileron;
			}
			if (Mathf.Abs(Acc.CS_yawRange(cs)) > Mathf.Abs(Acc.CS_rollRange(cs)))
			{
				return SurfaceKind.Rudder;
			}
			return SurfaceKind.Other;
		}

		private static AirfoilTable FoilFor(AeroPart part, AircraftParameters prm)
		{
			Airfoil af = null;
			int num = Acc.AP_airfoil(part);
			if (num >= 0 && prm != null && prm.airfoils != null && num < prm.airfoils.Length)
			{
				af = prm.airfoils[num];
			}
			return new AirfoilTable(af);
		}

		private void RebuildBodies()
		{
			bodies.Clear();
			foreach (UnitPart item in ac.partLookup)
			{
				if (!(item == null) && !(item.rb == null) && !item.IsDetached() && !bodies.Contains(item.rb))
				{
					bodies.Add(item.rb);
				}
			}
			if (bodies.Count == 0 && ac.rb != null)
			{
				bodies.Add(ac.rb);
			}
		}

		public void Dispose()
		{
			telemetry?.Close();
			telemetry = null;
		}

		private void UpdateMassProps(bool full, Quaternion rootInv)
		{
			float num = 0f;
			Vector3 zero = Vector3.zero;
			for (int num2 = bodies.Count - 1; num2 >= 0; num2--)
			{
				Rigidbody rigidbody = bodies[num2];
				if (rigidbody == null)
				{
					bodies.RemoveAt(num2);
				}
				else
				{
					num += rigidbody.mass;
					zero += rigidbody.mass * rigidbody.worldCenterOfMass;
				}
			}
			if (num < 1f)
			{
				return;
			}
			mass = num;
			cg = zero / num;
			if (!full)
			{
				return;
			}
			double[,] array = new double[3, 3];
			foreach (Rigidbody body in bodies)
			{
				Matrix4x4 matrix4x = Matrix4x4.Rotate(body.rotation * body.inertiaTensorRotation);
				Vector3 inertiaTensor = body.inertiaTensor;
				Vector3 vector = body.worldCenterOfMass - cg;
				float sqrMagnitude = vector.sqrMagnitude;
				for (int i = 0; i < 3; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						double num3 = matrix4x[i, 0] * inertiaTensor.x * matrix4x[j, 0] + matrix4x[i, 1] * inertiaTensor.y * matrix4x[j, 1] + matrix4x[i, 2] * inertiaTensor.z * matrix4x[j, 2];
						num3 += (double)(body.mass * (((i == j) ? sqrMagnitude : 0f) - vector[i] * vector[j]));
						array[i, j] += num3;
					}
				}
			}
			Matrix4x4 matrix4x2 = Matrix4x4.Rotate(rootInv);
			double[,] array2 = new double[3, 3];
			for (int k = 0; k < 3; k++)
			{
				for (int l = 0; l < 3; l++)
				{
					double num4 = 0.0;
					for (int m = 0; m < 3; m++)
					{
						for (int n = 0; n < 3; n++)
						{
							num4 += (double)matrix4x2[k, m] * array[m, n] * (double)matrix4x2[l, n];
						}
					}
					array2[k, l] = num4;
				}
			}
			int[] array3 = new int[3] { 2, 0, 1 };
			float[] array4 = new float[3] { -1f, -1f, 1f };
			for (int num5 = 0; num5 < 3; num5++)
			{
				for (int num6 = 0; num6 < 3; num6++)
				{
					Ia[num5, num6] = (float)((double)(array4[num5] * array4[num6]) * array2[array3[num5], array3[num6]]);
				}
			}
			Invert3(Ia, IaInv);
			alloc.SetInverseInertia(IaInv);
		}

		private static void Invert3(float[,] a, float[,] o)
		{
			float num = a[0, 0] * (a[1, 1] * a[2, 2] - a[1, 2] * a[2, 1]) - a[0, 1] * (a[1, 0] * a[2, 2] - a[1, 2] * a[2, 0]) + a[0, 2] * (a[1, 0] * a[2, 1] - a[1, 1] * a[2, 0]);
			if (Mathf.Abs(num) < 1E-06f)
			{
				for (int i = 0; i < 3; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						o[i, j] = ((i == j) ? (1f / Mathf.Max(1f, a[i, i])) : 0f);
					}
				}
				return;
			}
			float num2 = 1f / num;
			o[0, 0] = (a[1, 1] * a[2, 2] - a[1, 2] * a[2, 1]) * num2;
			o[0, 1] = (a[0, 2] * a[2, 1] - a[0, 1] * a[2, 2]) * num2;
			o[0, 2] = (a[0, 1] * a[1, 2] - a[0, 2] * a[1, 1]) * num2;
			o[1, 0] = (a[1, 2] * a[2, 0] - a[1, 0] * a[2, 2]) * num2;
			o[1, 1] = (a[0, 0] * a[2, 2] - a[0, 2] * a[2, 0]) * num2;
			o[1, 2] = (a[0, 2] * a[1, 0] - a[0, 0] * a[1, 2]) * num2;
			o[2, 0] = (a[1, 0] * a[2, 1] - a[1, 1] * a[2, 0]) * num2;
			o[2, 1] = (a[0, 1] * a[2, 0] - a[0, 0] * a[2, 1]) * num2;
			o[2, 2] = (a[0, 0] * a[1, 1] - a[0, 1] * a[1, 0]) * num2;
		}

		private Vector3 MulI(Vector3 v)
		{
			return new Vector3(Ia[0, 0] * v.x + Ia[0, 1] * v.y + Ia[0, 2] * v.z, Ia[1, 0] * v.x + Ia[1, 1] * v.y + Ia[1, 2] * v.z, Ia[2, 0] * v.x + Ia[2, 1] * v.y + Ia[2, 2] * v.z);
		}

		private static float[] BuildAsymAlphas()
		{
			float[] array = new float[22];
			for (int i = 0; i < 22; i++)
			{
				array[i] = -40f + 5f * (float)i;
			}
			return array;
		}

		// 1.8.35: past the normal rudder fade (RudderFadeStartAoA-EndAoA) the canted rudders' "inverse roll" is what
		// moves sideslip: beta-dot = p sin(a) - r cos(a), and a yaw-right rudder command rolls the jet left, so both terms
		// push beta the same way. Bench sweep: from ~55 deg AoA their sideslip effect is >3x their velocity-vector roll
		// (6x at 75), while the flaperons' and stabs' sideslip authority collapses near 60 and reverses past ~62.
		// So above the fade the rudders get a limited range that can only REDUCE the sideslip error: deflection only in
		// the beta-reducing direction (taken from the live surface model, so it follows the sign flip at negative AoA),
		// scaled in by the error (0 below 0.5 deg, full at 3 deg), and capped so the body yaw acceleration the rudders make
		// stays within RudderSideslipYawCap. Their yaw per degree falls with AoA (about 0 by 80 deg), so the travel opens
		// up as AoA rises. Uses the previous tick's surface model and pedal-sideslip command.
		private void SideslipRudder(float dt)
		{
			rudSideDbg = 0f;
			float cap = Plugin.RudderSideslipYawCap;
			float gate = (1f - rudderAuth) * ((Mathf.Abs(alpha) < 100f) ? 1f : 0f);
			SurfaceEffector rl = null;
			SurfaceEffector rr = null;
			foreach (SurfaceEffector se in surfaces)
			{
				if (se.kind == SurfaceKind.Rudder)
				{
					if (se.name.EndsWith("_L"))
					{
						rl = se;
					}
					else if (se.name.EndsWith("_R"))
					{
						rr = se;
					}
				}
			}
			if (cap <= 0f || gate <= 0.001f || !Engaged || !havePrev || Mode == FcsMode.Ground || Mode == FcsMode.Off || rl == null || rr == null || !rl.active || !rr.active || rl.N < 2 || rr.N < 2)
			{
				rudSideF = Mathf.MoveTowards(rudSideF, 0f, 60f * dt);
				return;
			}
			float sL = Mathf.Sign(rl.origYawRange);
			float sR = Mathf.Sign(rr.origYawRange);
			const float probe = 10f;
			Vector3 m = rl.MAt(sL * probe) + rr.MAt(sR * probe) - rl.MAt(0f) - rr.MAt(0f);
			float pDot = AccRoll(m) / probe;
			float rDot = AccYaw(m) / probe;
			float ar = alpha * (MathF.PI / 180f);
			float bdd = pDot * Mathf.Sin(ar) - rDot * Mathf.Cos(ar);
			float err = beta - betaPedalCmd;
			float y = 0f;
			if (Mathf.Abs(bdd) > 1E-4f)
			{
				float span = Mathf.Min(Mathf.Max(rl.max, 0f - rl.min), Mathf.Max(rr.max, 0f - rr.min));
				float lim = Mathf.Min(span, cap / Mathf.Max(Mathf.Abs(rDot), 1E-4f));
				float use = Mathf.Clamp01((Mathf.Abs(err) - 0.5f) / 2.5f);
				y = (0f - Mathf.Sign(err) * Mathf.Sign(bdd)) * lim * use * gate;
			}
			// servo-rate-limited so the allowed range cannot jump across zero in one frame when the error changes sign
			rudSideF = Mathf.MoveTowards(rudSideF, y, Mathf.Max(1f, rl.rate) * dt);
			rudSideDbg = rudSideF;
			float yLo = Mathf.Min(0f, rudSideF);
			float yHi = Mathf.Max(0f, rudSideF);
			ApplyYawInterval(rl, sL, yLo, yHi);
			ApplyYawInterval(rr, sR, yLo, yHi);
		}

		private static void ApplyYawInterval(SurfaceEffector se, float sgn, float yLo, float yHi)
		{
			float a = sgn * yLo;
			float b = sgn * yHi;
			float lo = Mathf.Min(a, b);
			float hi = Mathf.Max(a, b);
			se.lo = Mathf.Clamp(Mathf.Min(se.lo, lo), se.min, se.max);
			se.hi = Mathf.Clamp(Mathf.Max(se.hi, hi), se.min, se.max);
		}

		// 1.8.36 fly-by-wire ground steering. Outer loop: pedal -> yaw-rate command (GroundYawRateMax, limited to
		// GroundLateralAccel / V); pedal released -> yaw braking (zero-rate command) until |r| < 0.7 deg/s, then capture the
		// ground track (or the heading when nearly stopped) and the line through the CG, and hold that line: desired track =
		// captured track minus atan(cross-track / max(4 s x V, 25 m)) (<= 5 deg), desired heading = desired track (the heading
		// is lined up with the track: a touchdown crab or a weathervaning crosswind is steered out, the mains' side force
		// removes the drift). Inner loop: nose-wheel angle = kinematic feed-forward atan(r L / V) + yaw-rate P+I, limited to
		// what the tyre can use at that speed (NO's tyre saturates at 1/(0.2 + 0.01 v) deg of slip); in the ground roll the
		// rudders run the same yaw-rate error. Below 1.5 m/s the pedal steers the wheel directly.
		private void GroundSteer(float dt, bool mainsWow, bool noseWowWeight)
		{
			// contact rather than weight: in the ground roll the F-22E's nose leg carries only ~15-20 kN (about 0.02 m of
			// its 0.7 m travel), below WeightOnWheel(0.05), yet the tyre still steers with that load
			bool noseWow = false;
			bool anyContact = mainsWow;
			foreach (LandingGear g in gears)
			{
				if (g != null && g.WeightOnWheel(0.002f))
				{
					anyContact = true;
					if (g == NoseGear())
					{
						noseWow = true;
					}
				}
			}
			// human pilots only: AI taxi logic is written for the stock pedal-to-wheel-angle steering
			bool on = Plugin.NoseWheelFbw && !isAI && Engaged && Mode != FcsMode.Off && anyContact;
			if (!anyContact && ac.rb != null)
			{
				// airborne: remember the ground track (0.5 s filter) so a touchdown captures the approach track (the runway),
				// not the track the mains have already dragged toward a crabbed heading
				Vector3 va = ac.rb.velocity;
				if (va.x * va.x + va.z * va.z > 400f)
				{
					float ca = Mathf.Atan2(va.x, va.z) * 57.29578f;
					gsChiAir = ((gsAirT > 0f) ? (gsChiAir + Mathf.DeltaAngle(gsChiAir, ca) * (dt / (0.5f + dt))) : ca);
					gsAirT += dt;
				}
				else
				{
					gsAirT = 0f;
				}
			}
			if (!on)
			{
				gsActive = false;
				gsInt = 0f;
				gsSinceContact += dt;
				if (gsSinceContact < 1.5f && Plugin.NoseWheelFbw && !isAI)
				{
					// a bounce: keep the captured line and track
					gsNwsDeg = 0f;
					gsRud = 0f;
					gsMainsContact = false;
					return;
				}
				gsHold = false;
				gsRudInt = 0f;
				gsBrakeT = 0f;
				gsNwsDeg = 0f;
				gsRud = 0f;
				gsMainsContact = false;
				return;
			}
			LandingGear nose = NoseGear();
			float steerLock = ((nose != null) ? Mathf.Max(1f, Mathf.Abs(Acc.LG_steerLock(nose))) : 45f);
			if (gsLead < 0f)
			{
				gsLead = 5.9f;
				if (nose != null)
				{
					float best = float.MinValue;
					foreach (LandingGear g in gears)
					{
						if (g != null && g != nose)
						{
							best = Mathf.Max(best, Vector3.Dot(g.transform.position - nose.transform.position, -((Component)(object)ac).transform.forward));
						}
					}
					if (best > 1f && best < 30f)
					{
						gsLead = best;
					}
				}
			}
			Rigidbody rb = ac.rb;
			Vector3 v = ((rb != null) ? rb.velocity : Vector3.zero);
			Vector3 vh = new Vector3(v.x, 0f, v.z);
			float V = vh.magnitude;
			Vector3 fwd = ((Component)(object)ac).transform.forward;
			float psi = Mathf.Atan2(fwd.x, fwd.z) * 57.29578f;
			float chi = ((V > 0.5f) ? (Mathf.Atan2(vh.x, vh.z) * 57.29578f) : psi);
			float r = ((rb != null) ? (Vector3.Dot(rb.angularVelocity, Vector3.up) * 57.29578f) : 0f);
			Vector3 pos = ((rb != null) ? rb.worldCenterOfMass : ((Component)(object)ac).transform.position);
			float rLim = Mathf.Min(Plugin.GroundYawRateMax, Plugin.GroundLateralAccel / Mathf.Max(V, 0.5f) * 57.29578f);
			float ped = sy;
			float rc;
			gsSinceContact = 0f;
			if (!gsActive && !gsHold && gsAirT > 0.3f && V > 20f && Mathf.Abs(ped) <= 0.05f)
			{
				// touchdown: hold the approach track through the touchdown point straight away (drift / crab correction)
				gsHold = true;
				gsChiRef = gsChiAir;
				gsLineP = pos;
				gsRCmd = r;
			}
			if (anyContact)
			{
				gsAirT = 0f;
			}
			if (Mathf.Abs(ped) > 0.05f || V < 1.5f)
			{
				rc = ped * rLim;
				gsHold = false;
				gsBrakeT = 0f;
			}
			else
			{
				rc = 0f;
				if (!gsHold)
				{
					gsBrakeT += dt;
					if (Mathf.Abs(r) < 0.7f || gsBrakeT > 2f)
					{
						gsHold = true;
						gsChiRef = ((V > 3f) ? chi : psi);
						gsLineP = pos;
					}
				}
				if (gsHold)
				{
					float cr = gsChiRef * (MathF.PI / 180f);
					Vector3 right = new Vector3(Mathf.Cos(cr), 0f, 0f - Mathf.Sin(cr));
					gsLineY = Vector3.Dot(pos - gsLineP, right);
					float chiDes = gsChiRef - Mathf.Clamp(Mathf.Atan2(gsLineY * Mathf.Clamp01(Plugin.GroundLineHold), Mathf.Max(4f * V, 25f)) * 57.29578f, -5f, 5f);
					float e = Mathf.DeltaAngle(psi, chiDes);
					// corrections are not limited by the pedal's comfort limit (GroundLateralAccel), only by GsHoldRate
					float rHold = Mathf.Min(GsHoldRate, Plugin.GroundYawRateMax);
					rc = Mathf.Clamp(GsKpsi * e, 0f - rHold, rHold);
				}
			}
			// yaw-acceleration limit on the command so the nose tyre is not asked for more than it can grip at the entry
			gsRCmd = Mathf.MoveTowards(gsRCmd, rc, GsYawAccel * dt);
			rc = gsRCmd;
			float err = rc - r;
			if (noseWow && V >= 1.5f)
			{
				float ff = Mathf.Atan(rc * (MathF.PI / 180f) * gsLead / Mathf.Max(V, 1f)) * 57.29578f;
				float kp = Mathf.Clamp(GsKp0 / Mathf.Max(V, 3f), GsKpMin, GsKpMax);
				float lim = Mathf.Min(steerLock, GsLimK / Mathf.Max(V, 1f));
				// NO's tyre: lateral force = clamp(slip x (0.2 + 0.01 v), +-1) x muN, so it saturates at 1 / (0.2 + 0.01 v) deg of
				// slip. Feedback beyond that only drives the wheel into a skid and winds the integrator up.
				float sat = GsSatMargin / (0.2f + 0.01f * V);
				gsInt = Mathf.Clamp(gsInt + GsKi * kp * err * dt, 0f - sat, sat);
				float fb = Mathf.Clamp(kp * err + gsInt, 0f - sat, sat);
				float blend = Mathf.InverseLerp(1.5f, 3f, V);
				gsNwsDeg = Mathf.Lerp(ped * steerLock, Mathf.Clamp(ff + fb, 0f - lim, lim), blend);
			}
			else if (noseWow)
			{
				gsInt = 0f;
				gsNwsDeg = ped * steerLock;
			}
			else
			{
				gsInt = 0f;
				gsNwsDeg = 0f;
			}
			float rq = Mathf.InverseLerp(10f, 25f, V);
			gsRudInt = ((rq > 0f) ? Mathf.Clamp(gsRudInt + GsKri * err * dt * rq, -0.5f, 0.5f) : 0f);
			// below taxi speed the rudders have no authority: they show the pedal, as stock
			gsRud = Mathf.Lerp(ped, Mathf.Clamp(GsKr * err + gsRudInt, -1f, 1f), rq);
			gsMainsContact = mainsContactNow(nose);
			gsActive = true;
		}

		public bool gsMainsContact;

		private bool mainsContactNow(LandingGear nose)
		{
			foreach (LandingGear g in gears)
			{
				if (g != null && g != nose && g.WeightOnWheel(0.002f))
				{
					return true;
				}
			}
			return false;
		}

		public bool NoseSteerOverride(out float yaw)
		{
			LandingGear nose = NoseGear();
			float steerLock = ((nose != null) ? Mathf.Max(1f, Mathf.Abs(Acc.LG_steerLock(nose))) : 45f);
			yaw = Mathf.Clamp(gsNwsDeg / steerLock, -1f, 1f);
			return gsActive;
		}

		internal static float GsKpsi = 1f;

		internal static float GsHoldRate = 4f;

		internal static float GsYawAccel = 20f;

		internal static float GsSatMargin = 1.2f;

		internal static float GsKp0 = 15f;

		internal static float GsKpMin = 0.15f;

		internal static float GsKpMax = 3f;

		internal static float GsKi = 1f;

		internal static float GsLimK = 300f;

		internal static float GsKr = 0.1f;

		internal static float GsKphi = 3f;

		internal static float GsPhiFull = 10f;

		internal static float GsKri = 0.1f;

		private static float RudderAuthAt(float a)
		{
			float num = 1f - Mathf.Clamp01((Mathf.Abs(a) - Plugin.RudderFadeStart) / Mathf.Max(1f, Plugin.RudderFadeEnd - Plugin.RudderFadeStart));
			if (RudderRevBack)
			{
				num = Mathf.Max(num, Mathf.InverseLerp(120f, 140f, Mathf.Abs(a)));
			}
			return num;
		}

		private float AccRoll(Vector3 m)
		{
			return (IaInv[0, 0] * m.x + IaInv[0, 2] * m.z) * 57.29578f;
		}

		private float AccYaw(Vector3 m)
		{
			return (IaInv[2, 0] * m.x + IaInv[2, 2] * m.z) * 57.29578f;
		}

		private void AsymStep(float dt, float rho, Quaternion rootRot, ref EvalContext ctx, Vector3 wdF, Vector3 mF)
		{
			DecelStep(dt);
			if (havePrev)
			{
				Vector3 m = MulI(wdF) + Vector3.Cross(omega, MulI(omega)) - mF;
				dMeasR = dMeasRF.Step(AccRoll(m), 0.3f, dt);
				if (Mathf.Abs(omega.x * 57.29578f) < 20f)
				{
					dTrimR = dTrimRF.Step(AccRoll(m) - Lp * omega.x * IaInv[0, 0] * 57.29578f, 0.5f, dt);
				}
				dMeasY = dMeasYF.Step(AccYaw(m), 0.3f, dt);
			}
			if (!Plugin.AsymLimiter)
			{
				asymCapPos = (asymCapPosEff = 999f);
				asymCapNeg = (asymCapNegEff = -999f);
				asymCapFilt = 999f;
				asymCapFiltNeg = -999f;
				asymValid = false;
				asymFF = Vector3.zero;
				return;
			}
			AsymPrepSym(rootRot);
			FlapSymStep(dt);
			asymMNow = AsymMomentAt(alpha, rho, rootRot, ref ctx);
			Vector3 vector = asymMF.Step(asymMNow, Mathf.Max(0.05f, Plugin.AsymFFWashout), dt);
			float num = Mathf.Max(0.01f, Plugin.AsymDeadband);
			float num2 = Mathf.Clamp01(Mathf.Max(Mathf.Abs(AccRoll(asymMNow)) / num, Mathf.Abs(AccYaw(asymMNow)) / (0.25f * num)) - 1f);
			float num3 = 1f - Mathf.Clamp01((Mathf.Abs(beta) - Plugin.AsymFFMaxBeta) / 12f);
			float num4 = 1f - Mathf.Clamp01((Mathf.Abs(alpha) - Plugin.AsymFFMaxAoA) / 15f);
			asymFFGate = num2 * num3 * num4;
			asymFF = (0f - Mathf.Clamp01(Plugin.AsymFeedForward)) * asymFFGate * new Vector3(asymMNow.x - vector.x, 0f, asymMNow.z - vector.z);
			asymFFr = AccRoll(asymFF);
			asymFFy = AccYaw(asymFF);
			asymTimer += dt;
			if (asymK < 0 && asymTimer > 0.2f && ias > Plugin.AsymMinIas)
			{
				asymTimer = 0f;
				AsymSweepPrep(rootRot);
				asymK = 0;
			}
			if (asymK >= 0)
			{
				int num5 = Mathf.Min(asymAlphas.Length, asymK + AsymPointsPerStep);
				AsymSweepPoints(asymK, num5, rho, rootRot, ref ctx);
				asymK = ((num5 < asymAlphas.Length) ? num5 : (-1));
			}
			if (asymK < 0 && asymCycleDone)
			{
				asymCycleDone = false;
				asymCapPos = AsymCap.Find(asymAlphas, asymU, asymAlphas.Length, 1, Plugin.AoAPos, Mathf.Min(Plugin.AsymCapFloor, Plugin.AoAPos), AsymCapUMax);
				asymCapNeg = AsymCap.Find(asymAlphas, asymU, asymAlphas.Length, -1, Plugin.AoANeg, 0f - Mathf.Min(Plugin.AsymCapFloorNeg, 0f - Plugin.AoANeg), AsymCapUMax);
				asymCapPitch = 999f;
				float capGate = Mathf.Clamp01((Mathf.Abs(asymDy) - CoupHeldDy0) / CoupHeldDySpan);
				if (Plugin.AsymPitchMargin > 0f && capGate > 0.5f && asymCapPos < Plugin.AoAPos - 0.5f && Ia[1, 1] > 1f)
				{
					float tvDn = 0f;
					if (tvc.active && tvc.M != null)
					{
						float tvLo = float.MaxValue;
						for (int tk = 0; tk < tvc.N; tk++)
						{
							tvLo = Mathf.Min(tvLo, tvc.M[tk].y);
						}
						if (tvLo < float.MaxValue)
						{
							tvDn = tvLo * IaInv[1, 1] * 57.29578f;
						}
					}
					float prevA = 0f;
					float prevM = float.NaN;
					for (int ia = 0; ia < asymAlphas.Length; ia++)
					{
						float aA = asymAlphas[ia];
						if (aA < 0f)
						{
							continue;
						}
						float mDn = 0f - (asymNetDn[ia] + tvDn);
						if (mDn < Plugin.AsymPitchMargin)
						{
							float aP = (float.IsNaN(prevM) || prevM <= mDn) ? aA : Mathf.Lerp(prevA, aA, (prevM - Plugin.AsymPitchMargin) / (prevM - mDn));
							asymCapPitch = Mathf.Max(AsymPitchCapFloor, aP - AsymPitchCapLead);
							break;
						}
						prevA = aA;
						prevM = mDn;
					}
					asymCapPos = Mathf.Min(asymCapPos, asymCapPitch);
				}
				AuthCapFromSweep();
				PushCapSample(asymCapPos, asymCapNeg, !asymValid);
				if (!asymValid)
				{
					asymCapPosEff = asymCapPos;
					asymCapNegEff = asymCapNeg;
					asymValid = true;
				}
				if (!asymFirstLogged)
				{
					asymFirstLogged = true;
					Plugin.Log.LogInfo($"F-22E FCS: asymmetric-load check at {ias:F0} m/s IAS - lateral CG offset {asymDy * 100f:F1} cm, lateral usage at 30/45/60 deg AoA " + $"{AsymCap.Interp(asymAlphas, asymU, asymAlphas.Length, 30f):F2}/{AsymCap.Interp(asymAlphas, asymU, asymAlphas.Length, 45f):F2}/{AsymCap.Interp(asymAlphas, asymU, asymAlphas.Length, 60f):F2} " + $"(cap engages above 1.00); roll/yaw authority there {AsymCap.Interp(asymAlphas, asymCr, asymAlphas.Length, 30f):F0}/{AsymCap.Interp(asymAlphas, asymCy, asymAlphas.Length, 30f):F0}, " + $"{AsymCap.Interp(asymAlphas, asymCr, asymAlphas.Length, 45f):F0}/{AsymCap.Interp(asymAlphas, asymCy, asymAlphas.Length, 45f):F0}, " + $"{AsymCap.Interp(asymAlphas, asymCr, asymAlphas.Length, 60f):F0}/{AsymCap.Interp(asymAlphas, asymCy, asymAlphas.Length, 60f):F0} deg/s^2; " + $"load accel at 45 deg {AsymCap.Interp(asymAlphas, asymAr, asymAlphas.Length, 45f):F1}/{AsymCap.Interp(asymAlphas, asymAy, asymAlphas.Length, 45f):F1} deg/s^2 (deadband {Plugin.AsymDeadband:F1}); AoA cap {asymCapPos:F1}/{asymCapNeg:F1} deg.");
				}
			}
			if (asymValid)
			{
				CapFilterStep(dt);
				float upRate = Plugin.AsymRelax * (1f + Mathf.Max(0f, Plugin.AsymRelaxBoost) * (1f - Mathf.Clamp01(AsymCap.Interp(asymAlphas, asymU, asymAlphas.Length, asymCapPosEff))));
				asymCapPosEff = AsymCap.Slew(asymCapPosEff, asymCapFilt, 1, Plugin.AsymTighten, upRate, dt);
				asymCapNegEff = AsymCap.Slew(asymCapNegEff, asymCapFiltNeg, -1, Plugin.AsymTighten, upRate, dt);
				asymUNow = AsymCap.Interp(asymAlphas, asymU, asymAlphas.Length, alpha);
				asymArNow = AsymCap.Interp(asymAlphas, asymAr, asymAlphas.Length, alpha);
				asymAyNow = AsymCap.Interp(asymAlphas, asymAy, asymAlphas.Length, alpha);
				asymCrNow = AsymCap.Interp(asymAlphas, asymCr, asymAlphas.Length, alpha);
				asymCyNow = AsymCap.Interp(asymAlphas, asymCy, asymAlphas.Length, alpha);
				asymLogTimer += dt;
				bool flag = asymCapPos < Plugin.AoAPos - 0.1f || asymCapNeg > Plugin.AoANeg + 0.1f;
				bool flag2 = asymLoggedCap < Plugin.AoAPos - 0.1f || asymLoggedCapN > Plugin.AoANeg + 0.1f;
				if (asymLogTimer > 2f && (flag != flag2 || (flag && (Mathf.Abs(asymCapPos - asymLoggedCap) > 3f || Mathf.Abs(asymCapNeg - asymLoggedCapN) > 3f))))
				{
					asymLogTimer = 0f;
					asymLoggedCap = asymCapPos;
					asymLoggedCapN = asymCapNeg;
					Plugin.Log.LogInfo(flag ? $"F-22E FCS: asymmetric load - lateral CG offset {asymDy * 100f:F1} cm, AoA cap {asymCapPos:F1}/{asymCapNeg:F1} deg at {ias:F0} m/s IAS (lateral demand at 30 deg: {AsymCap.Interp(asymAlphas, asymU, asymAlphas.Length, 30f):F2}, at 60: {AsymCap.Interp(asymAlphas, asymU, asymAlphas.Length, 60f):F2}; load accel at 45 deg {AsymCap.Interp(asymAlphas, asymAr, asymAlphas.Length, 45f):F1} roll / {AsymCap.Interp(asymAlphas, asymAy, asymAlphas.Length, 45f):F1} yaw, authority there {AsymCap.Interp(asymAlphas, asymCr, asymAlphas.Length, 45f):F0} / {AsymCap.Interp(asymAlphas, asymCy, asymAlphas.Length, 45f):F0} deg/s^2)" : $"F-22E FCS: asymmetric-load AoA cap released (lateral CG offset {asymDy * 100f:F1} cm)");
				}
			}
		}

		// ---- 1.8.45: IAS-decay prediction and asymmetric-cap filter ----
		public float iasDotF;

		public float qPredK = 1f;

		public float latPredK = 1f;

		public float asymCapFilt = 999f;

		public float asymCapFiltNeg = -999f;

		private float iasPrevD = -1f;

		private readonly float[] capHist = new float[3];

		private readonly float[] capHistN = new float[3];

		private int capHistI;

		private float capMed = 999f;

		private float capMedNeg = -999f;

		internal static float BrakeRefIas => Plugin.YawBrakeRefKmh / 3.6f;   // 1.9.2: 270 km/h (was 280): the reference manoeuvre's IAS

		// 1.9.2: below YawMarginIasHi the yaw braking margin grows smoothly (smoothstep) to YawMarginLow x at YawMarginIasLo
		internal static float YawMarginIasHi => Plugin.YawMarginStartKmh;

		internal static float YawMarginIasLo => Plugin.YawMarginFullKmh;

		internal static float YawMarginLow => Plugin.YawMarginLowIas;

		internal static float BrakeSoftMin = 0.12f;      // soft-min width, fraction of the limits' mean

		public float yawMarginDbg = 1f;

		public float yawMidDbg = 1f;

		internal static float RollStopTau = 0.5f;

		internal static float ProtOnsetGain = 8f;

		internal static float ProtFlagBand = 1.5f;

		private float rollStopAF = -1f;

		// C1-smooth minimum: equals min(a, b) once they differ by more than k, at most k/4 below it where they cross
		private static float SoftMin(float a, float b, float k)
		{
			if (k <= 1e-4f)
			{
				return Mathf.Min(a, b);
			}
			float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
			return Mathf.Lerp(b, a, h) - k * h * (1f - h);
		}

		internal static float DecelTau = 0.25f;          // s, IAS-rate filter

		internal static float DecelLook => Plugin.DecelLookahead;   // s, how far ahead the decay is projected

		internal static float Decel0 => Plugin.DecelStart;          // m/s^2: below this, no anticipation

		internal static float Decel1 => Plugin.DecelFull;           // m/s^2: full weight from here (quadratic in between)

		internal static float DecelKMin = 0.35f;

		internal static float AsymResMaxFrac = 0.8f;

		// 1.8.45: demand at the cap (was 1.0). In-game, with the light mixed load (6.5 cm lateral CG offset, 60-110 m/s)
		// the demand rises ~0.025 per degree above the cap, so 1.08 is ~3 deg more AoA there; heavier loads rise faster.
		internal static float AsymCapUMax => Plugin.AsymCapDemand;

		internal static float AsymCapTauUp => Plugin.AsymCapSmoothing;

		internal static float AsymCapTauDown = 0.3f;

		internal static bool LatBrakeFloor => Plugin.HighAoABrakeFloor;

		internal static float LatBrakeA0 = 25f;

		internal static float LatBrakeA1 = 35f;

		internal static float LatBrakeMinRate = 8f;

		private void DecelStep(float dt)
		{
			if (dt <= 0f)
			{
				return;
			}
			if (iasPrevD < 0f || !Engaged)
			{
				iasPrevD = ias;
				iasDotF = 0f;
				qPredK = 1f;
				return;
			}
			float d = (ias - iasPrevD) / dt;
			iasPrevD = ias;
			iasDotF += (Mathf.Clamp(d, -80f, 80f) - iasDotF) * Mathf.Clamp01(dt / Mathf.Max(0.02f, DecelTau));
			float dec = Mathf.Max(0f, 0f - iasDotF);
			float w = Mathf.Clamp01((dec - Decel0) / Mathf.Max(0.1f, Decel1 - Decel0));
			float iasP = Mathf.Max(ias - dec * w * w * DecelLook, 0.1f * ias);
			float r = (ias > 1f) ? (iasP / ias) : 1f;
			qPredK = Mathf.Clamp(r * r, DecelKMin, 1f);
		}

		private void PushCapSample(float pos, float neg, bool reset)
		{
			if (reset)
			{
				for (int i = 0; i < 3; i++)
				{
					capHist[i] = pos;
					capHistN[i] = neg;
				}
				capMed = (asymCapFilt = pos);
				capMedNeg = (asymCapFiltNeg = neg);
				return;
			}
			capHist[capHistI] = pos;
			capHistN[capHistI] = neg;
			capHistI = (capHistI + 1) % 3;
			capMed = Med3(capHist[0], capHist[1], capHist[2]);
			capMedNeg = Med3(capHistN[0], capHistN[1], capHistN[2]);
		}

		private static float Med3(float a, float b, float c)
		{
			return Mathf.Max(Mathf.Min(a, b), Mathf.Min(Mathf.Max(a, b), c));
		}

		private void CapFilterStep(float dt)
		{
			// tightening passes quickly, relaxing slowly: noise can't lift the cap, a real drop is followed in ~0.3 s
			float kU = Mathf.Clamp01(dt / Mathf.Max(0.05f, AsymCapTauUp));
			float kD = Mathf.Clamp01(dt / Mathf.Max(0.05f, AsymCapTauDown));
			asymCapFilt += (capMed - asymCapFilt) * ((capMed < asymCapFilt) ? kD : kU);
			asymCapFiltNeg += (capMedNeg - asymCapFiltNeg) * ((capMedNeg > asymCapFiltNeg) ? kD : kU);
		}

		private void AsymPrepSym(Quaternion rootRot)
		{
			Vector3 rhs = rootRot * Vector3.right;
			Vector3 zero = Vector3.zero;
			int num = 0;
			foreach (SurfaceEffector surface in surfaces)
			{
				if (surface.partner != null && surface.active && surface.partner.active && surface.partner is SurfaceEffector surfaceEffector && !(surface.part.rb == null) && !(surfaceEffector.part.rb == null))
				{
					zero += 0.5f * (surface.part.rb.worldCenterOfMass + surfaceEffector.part.rb.worldCenterOfMass);
					num++;
				}
			}
			asymDy = ((num > 0) ? Vector3.Dot(cg - zero / num, rhs) : 0f);
			float[] array = asymDsym;
			if (array == null || array.Length != surfaces.Count)
			{
				array = (asymDsym = new float[surfaces.Count]);
			}
			for (int i = 0; i < surfaces.Count; i++)
			{
				array[i] = 0f;
			}
			for (int j = 0; j < surfaces.Count; j++)
			{
				SurfaceEffector surfaceEffector2 = surfaces[j];
				if (surfaceEffector2.partner != null)
				{
					int num2 = ((surfaceEffector2.partner is SurfaceEffector item) ? surfaces.IndexOf(item) : (-1));
					if (num2 >= 0)
					{
						float num3 = surfaceEffector2.pairSign * surfaceEffector2.now;
						float num4 = surfaceEffector2.partner.pairSign * surfaceEffector2.partner.now;
						float num5 = 0.5f * (num3 + num4);
						array[j] = surfaceEffector2.pairSign * (num3 - num5);
						array[num2] = surfaceEffector2.partner.pairSign * (num4 - num5);
					}
				}
			}
			if (surfaceParts.Count != 0)
			{
				return;
			}
			foreach (SurfaceEffector surface2 in surfaces)
			{
				if (surface2.part != null)
				{
					surfaceParts.Add(surface2.part);
				}
			}
		}

		private void AsymSweepPrep(Quaternion rootRot)
		{
			AsymPrepSym(rootRot);
			asymMT = (tvc.active ? tvc.Mnow : Vector3.zero);
		}

		private Vector3 AsymMomentAt(float aDeg, float rho, Quaternion rootRot, ref EvalContext ctx)
		{
			float[] array = asymDsym;
			if (array == null)
			{
				return Vector3.zero;
			}
			float num = Mathf.Max(tas, 1f);
			float f = aDeg * (MathF.PI / 180f);
			Vector3 vector = rootRot * new Vector3(0f, (0f - Mathf.Sin(f)) * num, Mathf.Cos(f) * num);
			Vector3 vector2 = vector / num;
			float num2 = 0.5f * rho * num * num;
			Vector3 zero = Vector3.zero;
			foreach (AeroPart aeroPart in aeroParts)
			{
				if (aeroPart == null || aeroPart.rb == null || aeroPart.IsDetached())
				{
					continue;
				}
				Transform transform = Acc.AP_liftNormal(aeroPart);
				if (!(transform == null))
				{
					Quaternion rotation = transform.rotation;
					Vector3 lhs = aeroPart.rb.worldCenterOfMass + rotation * Acc.AP_centerOfLift(aeroPart) - cg;
					Vector3 rhs = -vector2 * (0.5f * num2 * Acc.AP_dragArea(aeroPart));
					if (!surfaceParts.Contains(aeroPart) && foils.TryGetValue(aeroPart, out var value) && Acc.AP_wingArea(aeroPart) > 0f)
					{
						rhs += PartForce(vector, rotation, rotation * Vector3.right, value, 0.5f * rho * Acc.AP_wingArea(aeroPart) * Acc.AP_wingEff(aeroPart));
					}
					zero += Vector3.Cross(lhs, rhs);
				}
			}
			Vector3 vector3 = Quaternion.Inverse(rootRot) * zero;
			Vector3 result = new Vector3(0f - vector3.z, 0f - vector3.x, vector3.y) + (tvc.active ? tvc.Mnow : Vector3.zero);
			for (int i = 0; i < surfaces.Count; i++)
			{
				if (surfaces[i].active)
				{
					result += surfaces[i].EvalFlow(array[i], vector, rho, ref ctx);
				}
			}
			return result;
		}

		private void AsymSweepPoints(int k0, int k1, float rho, Quaternion rootRot, ref EvalContext ctx)
		{
			float[] array = asymDsym;
			if (array == null)
			{
				return;
			}
			float num = Mathf.Max(tas, 1f);
			Vector3 vector = asymMT;
			for (int i = k0; i < k1; i++)
			{
				float f = asymAlphas[i] * (MathF.PI / 180f);
				Vector3 vector2 = rootRot * new Vector3(0f, (0f - Mathf.Sin(f)) * num, Mathf.Cos(f) * num);
				Vector3 vector3 = vector2 / num;
				float num2 = 0.5f * rho * num * num;
				Vector3 zero = Vector3.zero;
				foreach (AeroPart aeroPart in aeroParts)
				{
					if (aeroPart == null || aeroPart.rb == null || aeroPart.IsDetached())
					{
						continue;
					}
					Transform transform = Acc.AP_liftNormal(aeroPart);
					if (!(transform == null))
					{
						Quaternion rotation = transform.rotation;
						Vector3 lhs = aeroPart.rb.worldCenterOfMass + rotation * Acc.AP_centerOfLift(aeroPart) - cg;
						Vector3 rhs = -vector3 * (0.5f * num2 * Acc.AP_dragArea(aeroPart));
						if (!surfaceParts.Contains(aeroPart) && foils.TryGetValue(aeroPart, out var value) && Acc.AP_wingArea(aeroPart) > 0f)
						{
							rhs += PartForce(vector2, rotation, rotation * Vector3.right, value, 0.5f * rho * Acc.AP_wingArea(aeroPart) * Acc.AP_wingEff(aeroPart));
						}
						zero += Vector3.Cross(lhs, rhs);
					}
				}
				Vector3 vector4 = Quaternion.Inverse(rootRot) * zero;
				Vector3 m = new Vector3(0f - vector4.z, 0f - vector4.x, vector4.y) + vector;
				float pAero = m.y - vector.y;
				float pStabMin = 0f;
				float pStabMax = 0f;
				float pFlapNow = 0f;
				float num3 = 0f;
				float num4 = 0f;
				float t = Mathf.InverseLerp(30f, 45f, Mathf.Abs(asymAlphas[i]));
				float num5 = RudderAuthAt(asymAlphas[i]);
				for (int j = 0; j < surfaces.Count; j++)
				{
					SurfaceEffector surfaceEffector = surfaces[j];
					if (!surfaceEffector.active)
					{
						continue;
					}
					m += surfaceEffector.EvalFlow(array[j], vector2, rho, ref ctx);
					if (surfaceEffector.kind == SurfaceKind.Flaperon || surfaceEffector.kind == SurfaceKind.Aileron)
					{
						pFlapNow += surfaceEffector.EvalFlow((flapSymRef != null && j < flapSymRef.Length) ? flapSymRef[j] : 0f, vector2, rho, ref ctx).y;
					}
					float num6;
					float num7;
					switch (surfaceEffector.kind)
					{
					case SurfaceKind.Flaperon:
					case SurfaceKind.Aileron:
						num6 = 1f;
						num7 = 1f;
						break;
					case SurfaceKind.Stabilator:
						num6 = (num7 = Mathf.Lerp(Plugin.StabRollShare, 1f, t));
						break;
					case SurfaceKind.Rudder:
						num6 = 0f;
						num7 = num5;
						break;
					default:
						num6 = (num7 = 0f);
						break;
					}
					if (num6 != 0f || num7 != 0f)
					{
						float num8 = float.MaxValue;
						float num9 = float.MinValue;
						float num10 = float.MaxValue;
						float num11 = float.MinValue;
						float pMinS = float.MaxValue;
						float pMaxS = float.MinValue;
						for (int l = 0; l < surfaceEffector.N; l++)
						{
							Vector3 m2 = surfaceEffector.EvalFlow(surfaceEffector.grid[l], vector2, rho, ref ctx);
							pMinS = Mathf.Min(pMinS, m2.y);
							pMaxS = Mathf.Max(pMaxS, m2.y);
							float b = AccRoll(m2);
							float b2 = AccYaw(m2);
							num8 = Mathf.Min(num8, b);
							num9 = Mathf.Max(num9, b);
							num10 = Mathf.Min(num10, b2);
							num11 = Mathf.Max(num11, b2);
						}
						num3 += num6 * 0.5f * (num9 - num8);
						num4 += num7 * 0.5f * (num11 - num10);
						if (surfaceEffector.kind == SurfaceKind.Stabilator)
						{
							pStabMin += pMinS;
							pStabMax += pMaxS;
						}
					}
				}
				asymNetDn[i] = (pAero + pStabMin) * IaInv[1, 1] * 57.29578f;
				authNetDn[i] = (pAero + pStabMin + pFlapNow) * IaInv[1, 1] * 57.29578f;
				asymNetUp[i] = (pAero + pStabMax) * IaInv[1, 1] * 57.29578f;
				asymAr[i] = AccRoll(m);
				asymAy[i] = AccYaw(m);
				asymCr[i] = num3;
				asymCy[i] = num4;
				// 1.8.45: the demand is judged at the dynamic pressure predicted from a rapid IAS decay. Load and authority
				// both scale with q and the absolute reserves (AsymRollReserveAccel / AsymYawReserveAccel) do not, so where
				// those bind, and through the fixed deadband everywhere, a fast deceleration brings the cap down ahead of
				// the authority loss. The share of the authority the reserve may claim is AsymResMaxFrac (0.8, as before).
				float kq = qPredK;
				float crK = kq * num3;
				float cyK = kq * num4;
				float num12 = Mathf.Clamp01(Plugin.AsymReserve);
				float tauRes = (Plugin.AsymRollTau > 0.01f) ? (Mathf.Max(0f, pStockRef) / Plugin.AsymRollTau) : 0f;
				float num13 = Mathf.Min(Mathf.Max(num12 * crK, Mathf.Max(tauRes, Plugin.AsymRollReserveAccel)), AsymResMaxFrac * crK);
				float num14 = Mathf.Min(Mathf.Max(num12 * cyK, Plugin.AsymYawReserveAccel), AsymResMaxFrac * cyK);
				float num15 = Mathf.Max(0f, Plugin.AsymDeadband);
				float num16 = 0.25f * num15;
				asymU[i] = Mathf.Max(0f, kq * Mathf.Abs(asymAr[i]) - num15) / Mathf.Max(crK - num13, 1f) + Mathf.Max(0f, kq * Mathf.Abs(asymAy[i]) - num16) / Mathf.Max(cyK - num14, 1f);
			}
			if (k1 >= asymAlphas.Length)
			{
				asymCycleDone = true;
			}
		}

		private float NonSurfacePitch(Vector3 wind, float rho, Quaternion rootInv, Vector3 rootRight, float dAlphaDeg, float qRad)
		{
			Quaternion rot = Quaternion.AngleAxis(dAlphaDeg, rootRight);
			Vector3 w = -rootRight * qRad;
			Vector3 m = Vector3.zero;
			foreach (AeroPart aeroPart in aeroParts)
			{
				if (aeroPart == null || aeroPart.rb == null || aeroPart.IsDetached() || surfaceParts.Contains(aeroPart))
				{
					continue;
				}
				Transform transform = Acc.AP_liftNormal(aeroPart);
				float area = Acc.AP_wingArea(aeroPart);
				if (transform == null || area <= 0f || !foils.TryGetValue(aeroPart, out var foil))
				{
					continue;
				}
				Quaternion rotation = transform.rotation;
				Vector3 arm = aeroPart.rb.worldCenterOfMass - cg;
				Vector3 r = arm + rotation * Acc.AP_centerOfLift(aeroPart);
				// flow at the part: the CG's air velocity turned by dAlpha, plus the pitch rotation qRad
				Vector3 v = rot * (vCgAir) + Vector3.Cross(w, arm);
				m += Vector3.Cross(r, PartForce(v, rotation, rotation * Vector3.right, foil, 0.5f * rho * area * Acc.AP_wingEff(aeroPart)));
			}
			return 0f - (rootInv * m).x;
		}

		private Vector3 vCgAir;

		private void ComputeHybridSlopes(Vector3 wind, float rho, Vector3 rootRight)
		{
			if (surfaceParts.Count == 0)
			{
				foreach (SurfaceEffector se in surfaces)
				{
					if (se.part != null)
					{
						surfaceParts.Add(se.part);
					}
				}
			}
			Quaternion rootInv = Quaternion.Inverse(((Component)(object)ac).transform.rotation);
			vCgAir = ac.rb.velocity - wind;
			float q0 = omega.y;
			float mP = NonSurfacePitch(wind, rho, rootInv, rootRight, 1f, q0);
			float mM = NonSurfacePitch(wind, rho, rootInv, rootRight, -1f, q0);
			float mQ1 = NonSurfacePitch(wind, rho, rootInv, rootRight, 0f, q0 + 0.05f);
			float mQ0 = NonSurfacePitch(wind, rho, rootInv, rootRight, 0f, q0 - 0.05f);
			mAlphaO = 0.5f * (mP - mM);
			mQO = (mQ1 - mQ0) / (0.1f * 57.29578f);
		}

		private void ComputeEnvelope(Vector3 wind, float rho, Vector3 rootRight, Vector3 eL)
		{
			for (int i = 0; i < envLift.Length; i++)
			{
				envLift[i] = 0f;
			}
			Quaternion quaternion = Quaternion.Inverse(((Component)(object)ac).transform.rotation);
			Vector3 lhs = -((Component)(object)ac).transform.forward * 0.2f;
			float num = 0f;
			float num2 = 0f;
			float num3 = mass * 9.81f;
			foreach (AeroPart aeroPart in aeroParts)
			{
				if (aeroPart == null || aeroPart.rb == null)
				{
					continue;
				}
				Transform transform = Acc.AP_liftNormal(aeroPart);
				float num4 = Acc.AP_wingArea(aeroPart);
				if (transform == null || aeroPart.IsDetached() || num4 <= 0f || !foils.TryGetValue(aeroPart, out var value))
				{
					continue;
				}
				Vector3 vector = aeroPart.rb.velocity - wind;
				float sqrMagnitude = vector.sqrMagnitude;
				if (!(sqrMagnitude < 25f))
				{
					float qS = 0.5f * rho * sqrMagnitude * num4 * Acc.AP_wingEff(aeroPart);
					Quaternion rotation = transform.rotation;
					Vector3 right = rotation * Vector3.right;
					Vector3 lhs2 = aeroPart.rb.worldCenterOfMass + rotation * Acc.AP_centerOfLift(aeroPart) - cg;
					Vector3 v = vector + Vector3.Cross(lhs, aeroPart.rb.worldCenterOfMass - cg);
					num += 0f - (quaternion * Vector3.Cross(lhs2, PartForce(vector, rotation, right, value, 0.5f * rho * num4 * Acc.AP_wingEff(aeroPart)))).z;
					num2 += 0f - (quaternion * Vector3.Cross(lhs2, PartForce(v, rotation, right, value, 0.5f * rho * num4 * Acc.AP_wingEff(aeroPart)))).z;
					for (int j = 0; j < envAlphas.Length; j++)
					{
						Quaternion quaternion2 = Quaternion.AngleAxis((j < 3) ? envAlphas[j] : (envAlphas[j] - alpha), rootRight);
						envLift[j] += PartLift(quaternion2 * vector, rotation, right, value, qS, eL);
					}
				}
			}
			nAlpha = Mathf.Clamp((envLift[2] - envLift[0]) * 0.5f / num3, 0.005f, 12f);
			Lp = Mathf.Min((num2 - num) / 0.2f, -1f);
			float num5 = float.MinValue;
			float num6 = 30f;
			for (int k = 3; k < EnvNegStart; k++)
			{
				float num7 = envLift[k] / num3 + tw * Mathf.Sin(envAlphas[k] * (MathF.PI / 180f));
				if (num7 > num5)
				{
					num5 = num7;
					num6 = envAlphas[k];
				}
			}
			nMaxAero = Mathf.Max(1f, num5);
			float nl = 0f;
			for (int kl = 3; kl < EnvNegStart; kl++)
			{
				nl = Mathf.Max(nl, envLift[kl] / num3);
			}
			nLiftMax = nl;
			float nlN = 0f;
			for (int kn = EnvNegStart; kn < envAlphas.Length; kn++)
			{
				nlN = Mathf.Max(nlN, (0f - envLift[kn]) / num3);
			}
			nLiftMin = nlN;
			alphaAtNMax = num6;
		}

		private static Vector3 PartForce(Vector3 v, Quaternion lr, Vector3 right, AirfoilTable t, float qSr)
		{
			float sqrMagnitude = v.sqrMagnitude;
			if (sqrMagnitude < 1f)
			{
				return Vector3.zero;
			}
			Vector3 vector = Quaternion.Inverse(lr) * v;
			t.Get(Mathf.Atan2(vector.y, vector.z), out var CL, out var CD);
			Vector3 vector2 = Vector3.Cross(v, right);
			float magnitude = vector2.magnitude;
			if (magnitude < 0.0001f)
			{
				return Vector3.zero;
			}
			float num = qSr * sqrMagnitude;
			return -vector2 / magnitude * (CL * num) - v / Mathf.Sqrt(sqrMagnitude) * (CD * num);
		}

		private float StockRollRef()
		{
			float num = 0.01868f * tas;
			float b = tas * tas * Mathf.Max(0.01f, ac.airDensity) / 24010f;
			float num2 = 1f / Mathf.Max(1f, b);
			float num3 = Mathf.Min(num, num * (1f + 4.5f * num2) / (1f + 1.1f * num2 * num)) * 57.29578f;
			float num4 = Mathf.Lerp(Plugin.RollGainLowIAS, Plugin.RollGainHighIAS, Mathf.InverseLerp(120f, 170f, ias));
			return num3 * num4;
		}

		private float NPath(float a)
		{
			float W = mass * 9.81f;
			if (a <= 5f)
			{
				return Nk(3) + nAlpha * (a - 5f);
			}
			if (a >= 65f)
			{
				return Nk(15);
			}
			float num = (a - 5f) / 5f;
			int num2 = Mathf.Clamp((int)num, 0, 11);
			float num3 = num - (float)num2;
			return Nk(3 + num2) + (Nk(4 + num2) - Nk(3 + num2)) * num3;
			float Nk(int k)
			{
				return envLift[k] / W + tw * Mathf.Sin(envAlphas[k] * (MathF.PI / 180f));
			}
		}

		private static float PartLift(Vector3 v, Quaternion lr, Vector3 right, AirfoilTable t, float qS, Vector3 eL)
		{
			Vector3 vector = Quaternion.Inverse(lr) * v;
			float a = Mathf.Atan2(vector.y, vector.z);
			t.Get(a, out var CL, out var _);
			Vector3 vector2 = Vector3.Cross(v, right);
			float magnitude = vector2.magnitude;
			if (magnitude < 0.0001f)
			{
				return 0f;
			}
			return Vector3.Dot(-vector2 / magnitude * (CL * qS), eL);
		}

		public void Tick(ControlInputs inp, bool flightAssist)
		{
			float num = Mathf.Max(0.001f, Time.fixedDeltaTime);
			time += num;
			Rigidbody rb = ac.rb;
			Transform transform = ((Component)(object)ac).transform;
			if (rb == null || transform == null)
			{
				return;
			}
			Quaternion rotation = transform.rotation;
			Quaternion quaternion = Quaternion.Inverse(rotation);
			massTimer += num;
			bool flag = massTimer > 0.5f;
			if (flag)
			{
				massTimer = 0f;
				RebuildBodies();
			}
			UpdateMassProps(flag || !havePrev, quaternion);
			Vector3 vector = Acc.AC_wind(ac);
			Vector3 vector2 = rb.velocity - vector;
			tas = vector2.magnitude;
			float num2 = Mathf.Max(0.01f, ac.airDensity);
			ias = tas * Mathf.Sqrt(num2 / 1.225f);
			Vector3 vector3 = quaternion * vector2;
			alpha = ((tas > 1f) ? (Mathf.Atan2(0f - vector3.y, vector3.z) * 57.29578f) : 0f);
			beta = ((tas > 1f) ? (Mathf.Asin(Mathf.Clamp(vector3.x / tas, -1f, 1f)) * 57.29578f) : 0f);
			Vector3 vector4 = quaternion * rb.angularVelocity;
			omega = new Vector3(0f - vector4.z, 0f - vector4.x, vector4.y);
			Vector3 x = (havePrev ? ((rb.velocity - velPrev) / num) : Vector3.zero);
			Vector3 vector5 = accF.Step(x, 0.05f, num);
			Vector3 lhs = vector5 - Physics.gravity;
			nz = Vector3.Dot(lhs, transform.up) / 9.81f;
			Vector3 vector6 = ((tas > 1f) ? Vector3.Cross(vector2, transform.right).normalized : transform.up);
			eLy = vector6.y;
			float vc = Mathf.Max(tas, 30f);
			omegaV = Vector3.Dot(vector5, vector6) / Mathf.Max(tas, 5f) * 57.29578f;
			vDot = vDotF.Step((tas > 1f) ? Vector3.Dot(vector5, vector2 / tas) : 0f, 0.2f, num);
			theta = Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * 57.29578f;
			phi = Mathf.Atan2(0f - transform.right.y, transform.up.y) * 57.29578f;
			float x2 = (havePrev ? (Mathf.DeltaAngle(alphaPrev, alpha) / num) : 0f);
			alphaDot = alphaDotF.Step(x2, 0.05f, num);
			alphaPrev = alpha;
			alphaU = ((Mathf.Abs(alpha) < 90f || !havePrev) ? alpha : Mathf.Clamp(alphaU + Mathf.DeltaAngle(alphaU, alpha), -300f, 300f));
			bool flag2 = false;
			foreach (LandingGear gear in gears)
			{
				if (gear != null && gear.WeightOnWheel(0.05f))
				{
					flag2 = true;
					break;
				}
			}
			flaps.Update(num, this, flag2 && tas < 110f, ac.speed, Mathf.Clamp(inp.roll, -1f, 1f), groundBrake);
			rudderAuth = 1f - Mathf.Clamp01((Mathf.Abs(alpha) - Plugin.RudderFadeStart) / Mathf.Max(1f, Plugin.RudderFadeEnd - Plugin.RudderFadeStart));
			if (RudderRevBack)
			{
				rudderAuth = Mathf.Max(rudderAuth, Mathf.InverseLerp(120f, 140f, Mathf.Abs(alpha)));
			}
			foreach (SurfaceEffector surface in surfaces)
			{
				float num3 = ((surface.kind == SurfaceKind.Rudder) ? rudderAuth : 1f);
				surface.lo = surface.min * num3;
				surface.hi = surface.max * num3;
			}
			SideslipRudder(num);
			EvalContext ctx = new EvalContext
			{
				cg = cg,
				rootInv = quaternion,
				rho = num2,
				wind = vector
			};
			Vector3 zero = Vector3.zero;
			RollPitchGateStep(num);
			foreach (Effector eff in effs)
			{
				if (eff is SurfaceEffector seH)
				{
					seH.honestGate = Mathf.Max(Plugin.OvershootPitchPriority ? pitchNeed : 0f, HonestRollGate * rpLat);
				}
				eff.Evaluate(ref ctx);
				if (eff.active)
				{
					zero += eff.Mnow;
					if (!Engaged || !havePrev)
					{
						eff.cmd = eff.now;
						eff.cmdOut = eff.now;
						eff.cmdLag = eff.now;
						eff.outTraj.Reset(eff.now);
					}
				}
			}
			Vector3 x3 = (havePrev ? ((omega - omegaPrev) / num) : Vector3.zero);
			Vector3 vector7 = omegaDotF.Step(x3, IndiTau, num);
			Vector3 vector8 = mNowF.Step(zero, IndiTau, num);
			float aIndiF = alphaIndiF.Step(alpha, IndiTau, num);
			float qIndiFv = qIndiF.Step(omega.y * 57.29578f, IndiTau, num);
			// faded out from 25 to 40 deg AoA: the lag matters at high dynamic pressure, and the stalled-foil slopes up there cost roll (bench, full pull + roll at 350 km/h, 64 deg: bank at 1 s 60 -> 48 deg without the fade)
			hybridDM = (1f - Mathf.InverseLerp(25f, 40f, Mathf.Abs(alpha))) * ((Mathf.Abs(alpha) < 80f && tas > 30f) ? (mAlphaO * Mathf.Clamp(alpha - aIndiF, -5f, 5f) + mQO * Mathf.Clamp(omega.y * 57.29578f - qIndiFv, -20f, 20f)) : 0f);
			omegaPrev = omega;
			velPrev = rb.velocity;
			havePrev = true;
			float num4 = 0f;
			float num5 = 0f;
			Vector3 zero2 = Vector3.zero;
			Vector3 zero3 = Vector3.zero;
			foreach (Effector eff2 in effs)
			{
				if (eff2.active)
				{
					Vector3 vector9 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
					Vector3 vector10 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
					for (int i = 0; i < eff2.N; i++)
					{
						vector9 = Vector3.Max(vector9, eff2.M[i]);
						vector10 = Vector3.Min(vector10, eff2.M[i]);
					}
					zero2 += vector9;
					zero3 += vector10;
				}
			}
			num4 = zero2.y;
			num5 = zero3.y;
			float num6 = IaInv[1, 1];
			aUp = Mathf.Clamp(aUpF.Step((vector7.y + (num4 - vector8.y) * num6) * 57.29578f, 0.15f, num), 5f, 1500f);
			aDn = Mathf.Clamp(aDnF.Step((0f - (vector7.y + (num5 - vector8.y) * num6)) * 57.29578f, 0.15f, num), 5f, 1500f);
			float num7 = 0f;
			float num8 = 0f;
			rollSlew = 100f;
			foreach (SurfaceEffector surface2 in surfaces)
			{
				if (!surface2.active)
				{
					continue;
				}
				float num9 = ((surface2.kind == SurfaceKind.Flaperon || surface2.kind == SurfaceKind.Aileron) ? 1f : ((surface2.kind == SurfaceKind.Stabilator) ? Mathf.Lerp(Plugin.StabRollShare, 0.25f, Mathf.InverseLerp(160f, 240f, ias)) : 0f));
				if (num9 != 0f)
				{
					float num10 = float.MinValue;
					float num11 = float.MaxValue;
					for (int j = 0; j < surface2.N; j++)
					{
						num10 = Mathf.Max(num10, surface2.M[j].x);
						num11 = Mathf.Min(num11, surface2.M[j].x);
					}
					num7 += num9 * 0.5f * (num10 - num11);
					if (surface2.kind != SurfaceKind.Stabilator)
					{
						num8 += 0.5f * (num10 - num11);
					}
					if (surface2.kind != SurfaceKind.Stabilator && surface2.rate > 0f)
					{
						rollSlew = Mathf.Min(rollSlew, surface2.rate / Mathf.Max(1f, 0.5f * (surface2.max - surface2.min)));
					}
				}
			}
			aRoll = Mathf.Clamp(aRollF.Step(num7 * IaInv[0, 0] * 57.29578f, 0.2f, num), 10f, 5000f);
			rollCtrlHalf = aRoll * (MathF.PI / 180f) / Mathf.Max(IaInv[0, 0], 1E-09f);
			aRollWing = Mathf.Clamp(aRollWF.Step(num8 * IaInv[0, 0] * 57.29578f, 0.2f, num), 10f, 5000f);
			aYaw = Mathf.Clamp(aYawF.Step(0.5f * (zero2.z - zero3.z) * IaInv[2, 2] * 57.29578f, 0.2f, num), 5f, 3000f);
			float t = Mathf.Clamp01((Mathf.Abs(alpha) - 20f) / 20f);
			float num12 = ((!flightAssist) ? Plugin.MpoGPos : Plugin.GPos);
			float num13 = ((!flightAssist) ? Plugin.MpoGNeg : Plugin.GNeg);
			float t2 = Mathf.Clamp01(Mathf.Max((nz - 0.85f * num12) / (0.15f * num12), (0.85f * num13 - nz) / (0.15f * (0f - num13))));
			stabPitchFrac = 1f;
			foreach (SurfaceEffector surface3 in surfaces)
			{
				if (surface3.kind == SurfaceKind.Stabilator && surface3.active)
				{
					stabPitchFrac = Mathf.Min(stabPitchFrac, surface3.pitchFrac);
				}
			}
			float t3 = 1f - stabPitchFrac;
			pitchNeed = 0f;
			if (Plugin.OvershootPitchPriority && Engaged && Mode != FcsMode.Ground && Mode != FcsMode.Off)
			{
				float num47 = Mathf.Sign(alpha) * (omega.y * 57.29578f - qT);
				float pnA0 = 15f;
				float pnRamp = 10f;
				if (Plugin.AsymRollCoupling && AsymPitchPrio > 0f && alpha > 0f && asymCapPosEff < 60f)
				{
					float pnG = AsymPitchPrio * Mathf.Clamp01((Mathf.Abs(asymDy) - CoupHeldDy0) / CoupHeldDySpan);
					pnA0 = Mathf.Lerp(15f, Mathf.Min(15f, asymCapPosEff - AsymPitchPrioLead), pnG);
					pnRamp = Mathf.Lerp(10f, 4f, pnG);
				}
				pitchNeed = Mathf.Clamp01((num47 - 2f) / 8f) * Mathf.Clamp01((Mathf.Abs(alpha) - pnA0) / pnRamp);
				if (prot)
				{
					pitchNeed = 1f;
				}
				t3 *= 1f - pitchNeed;
			}
			alloc.axisWeight = new Vector3(Mathf.Lerp(Mathf.Lerp(1f, 0.35f, t), 1.5f, t3), Mathf.Lerp(Mathf.Lerp(8f, 15f, t2), 1f, t3), Mathf.Lerp(Mathf.Lerp(Plugin.YawPriority, Mathf.Max(2f, Plugin.YawPriority), t), Mathf.Max(2.5f, Plugin.YawPriority), t3));
			alloc.PairedTrimSearch = PairedTrim && Plugin.StabTvcPairedSearch;
			// active while the pedals are in (and FightPedalRelease s after): rolls without pedal allocate as in 1.8.40
			float pedAct = Mathf.Clamp01((Mathf.Abs(sy) - 0.03f) / 0.15f);
			fightPed = Mathf.Max(pedAct, fightPed - num / Mathf.Max(0.05f, FightPedalRelease));
			float rollIn = Mathf.Clamp01((Mathf.Abs(sr) - 0.05f) / 0.2f);
			fightRollHold = Mathf.Max(rollIn, fightRollHold - num / Mathf.Max(0.05f, FightRollRelease));
			float fightGate = (FightMode == 1) ? 1f : ((FightMode == 2) ? Mathf.Max(fightPed, 1f - fightRollHold) : fightPed);
			alloc.fightK = ((Engaged && Mode != FcsMode.Ground && Mode != FcsMode.Off) ? (Mathf.Max(0f, Plugin.RollFightCost) * fightGate) : 0f);
			// 1.8.42: stab-vs-nozzle pitch fight, while a roll is commanded (and RpRelease after)
			// not while the pitch-rate command is moving (captures, releases: the nozzles are the fast pitch effector there),
			// in AoA protection or on an AoA run-away
			pfQHold = Mathf.Max(Mathf.Clamp01(Mathf.Abs(qTraj.v) / Mathf.Max(1f, PitchFightQdot)), pfQHold - num / Mathf.Max(0.05f, PitchFightQHold));
			// and not while the nose is coming up faster than commanded: the nozzles have to be free to stop it
			float pfErr = Mathf.Sign(alpha) * (omega.y * 57.29578f - qT);
			if (PitchFightAbsErr) pfErr = Mathf.Abs(pfErr);
			float pfGate = rpLat * (1f - pfQHold) * (prot ? 0f : (1f - pitchNeed)) * (1f - Mathf.Clamp01((pfErr - PitchFightErr0) / Mathf.Max(0.5f, PitchFightErrSpan)));
			alloc.pitchFightSign = ((alpha >= 0f) ? 1f : (-1f));
			alloc.pitchFightK = ((Engaged && Mode != FcsMode.Ground && Mode != FcsMode.Off) ? (Mathf.Max(0f, Plugin.RollPitchFightCost) * pfGate) : 0f);
			float num14 = 1f + Mathf.Max(0f, Plugin.AsymAxisBoost) * Mathf.Clamp01((asymUNow - 0.5f) / 0.5f);
			if (num14 > 1.001f)
			{
				alloc.axisWeight = new Vector3(alloc.axisWeight.x * num14, alloc.axisWeight.y, alloc.axisWeight.z * num14);
			}
			foreach (SurfaceEffector surface4 in surfaces)
			{
				if (surface4.kind == SurfaceKind.Stabilator && surface4.partner != null)
				{
					surface4.antiCost = Mathf.Lerp(0.0002f, Mathf.Lerp(Plugin.StabRollCost, 0.01f, Mathf.InverseLerp(160f, 240f, ias)), stabPitchFrac);
					surface4.antiCost = Mathf.Lerp(surface4.antiCost, Mathf.Max(surface4.antiCost, AsymStabAntiCost), pitchNeed * Mathf.Clamp01((Mathf.Abs(asymDy) - CoupHeldDy0) / CoupHeldDySpan));
					surface4.symCost = Plugin.StabTrimCost * (1f + LatTrimGain * latReserve);
				}
			}
			float relStick = 1f - Mathf.Clamp01((Mathf.Abs(sp) - 0.03f) / 0.15f);
			float relErr = Mathf.Clamp01((Mathf.Abs(omega.y * 57.29578f - qT) - 1f) / 8f);
			pitchPrio = Mathf.Lerp(1f, Mathf.Max(1f, Plugin.ReleasedPitchPriority), stabPitchFrac * relStick * relErr);
			if (pitchPrio > 1.001f)
			{
				alloc.axisWeight = new Vector3(alloc.axisWeight.x, alloc.axisWeight.y * pitchPrio, alloc.axisWeight.z);
			}
			float relRb = relStick * stabPitchFrac;
			stabRateBound = Engaged && Mode != FcsMode.Ground && Mode != FcsMode.Off && Plugin.ReleasedStabRateBound > 0f && relRb > 0.01f;
			if (stabRateBound)
			{
				foreach (SurfaceEffector surface12 in surfaces)
				{
					if (surface12.kind == SurfaceKind.Stabilator && surface12.active)
					{
						float num46 = Mathf.Max(1f, surface12.rate) * Plugin.ReleasedStabRateBound / relRb;
						surface12.lo = Mathf.Max(surface12.lo, Mathf.Min(surface12.now, surface12.hi) - num46);
						surface12.hi = Mathf.Min(surface12.hi, Mathf.Max(surface12.now, surface12.lo) + num46);
					}
				}
			}
			// 1.8.40: while rolling at low/moderate AoA, the stabs are not driven past their lift peak in any pass: beyond
			// it the down-going stab's lift falls off and its nose-down turns into pitch-up exactly when it rolls hardest.
			if (RpMainStall && (StabWindowAlways || rpGate > 0.01f) && Mathf.Abs(alpha) < RpMainAoA && Engaged && Mode != FcsMode.Ground && Mode != FcsMode.Off)
			{
				foreach (SurfaceEffector sw in surfaces)
				{
					if (sw.kind == SurfaceKind.Stabilator && sw.active)
					{
						StallWindow(sw, sw.stallAoA + RpMainStallMargin);
					}
				}
			}
			nAlphaTimer += num;
			if (nAlphaTimer > 0.1f)
			{
				nAlphaTimer = 0f;
				ComputeEnvelope(vector, num2, transform.right, vector6);
				ComputeHybridSlopes(vector, num2, transform.right);
			}
			AsymStep(num, num2, rotation, ref ctx, vector7, vector8);
			tw = tvc.totalThrust / (mass * 9.81f);
			float num15 = 0.5f * num2 * tas * tas;
			float num16 = 13781.25f;
			float num17 = Mathf.Clamp01(num15 / num16);
			num17 *= num17;
			float t4 = Mathf.Clamp01(Plugin.LowQRegRelief) * Mathf.InverseLerp(40f, 60f, ias);
			alloc.regScale = Mathf.Lerp(1f, Mathf.Max(0.02f, num17), t4);
			if (!revFlow && Mathf.Abs(alpha) > 100f)
			{
				revFlow = true;
			}
			else if (revFlow && Mathf.Abs(alpha) < 80f)
			{
				revFlow = false;
			}
			if (revFlow != revFlowLogged)
			{
				revFlowLogged = revFlow;
				Plugin.Log.LogInfo(revFlow ? "F-22E FCS: reversed flow (flying backwards) - roll sense flipped to body axes." : "F-22E FCS: forward flow.");
			}
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			LandingGear landingGear = NoseGear();
			foreach (LandingGear gear2 in gears)
			{
				if (!(gear2 == null) && gear2.WeightOnWheel(0.05f))
				{
					flag3 = true;
					if (gear2 == landingGear)
					{
						flag5 = true;
					}
					else
					{
						flag4 = true;
					}
				}
			}
			groundFrames = (flag3 ? Mathf.Min(groundFrames + 1, 1000) : 0);
			onGround = flag3;
			airTime = (flag3 ? 0f : (airTime + num));
			bool flag6 = flag3 || airTime < 0.2f;
			noseUpTime = ((landingGear != null && flag4 && !flag5 && tas > Plugin.RotationMinSpeed) ? (noseUpTime + num) : 0f);
			if (noseUpTime > 0.15f)
			{
				rotating = true;
				noseDownTime = 0f;
			}
			else if (rotating)
			{
				noseDownTime = (flag5 ? (noseDownTime + num) : 0f);
				if (!flag3 || noseDownTime > 0.3f || tas < Plugin.RotationMinSpeed - 8f)
				{
					rotating = false;
				}
			}
			mainsOnGround = flag4;
			FcsMode fcsMode = ((rotating && flag4) ? FcsMode.GearDownRate : ((flag6 && (tas < 110f || groundFrames > 10)) ? FcsMode.Ground : ((!flightAssist) ? FcsMode.MPO : ((!ac.gearDeployed) ? (isAI ? FcsMode.AIRate : FcsMode.AoAG) : FcsMode.GearDownRate))));
			if (fcsMode != Mode)
			{
				if (Mode == FcsMode.Ground || Mode == FcsMode.Off)
				{
					qT = omega.y * 57.29578f;
					qTprev = qT;
					qTraj.Reset(qT);
					pTraj.Reset(0f);
					pS = 0f;
					aInt = 0f;
					gInt = 0f;
				}
				Mode = fcsMode;
			}
			if (Mode != lastLoggedMode)
			{
				Plugin.Log.LogInfo("F-22E FCS mode: " + Mode);
				lastLoggedMode = Mode;
			}
			gbLogTimer += num;
			if (Plugin.GroundBrakeLog && flag3 && gbLogTimer > 1f)
			{
				gbLogTimer = 0f;
				LandingGear landingGear2 = NoseGear();
				Plugin.Log.LogInfo(string.Format("F-22E FCS ground brake: mode {0}, mainsWow {1}, noseWow {2} (nose gear {3}), ", Mode, flag4, flag5, (landingGear2 != null) ? landingGear2.name : "none") + $"tas {tas:F1} (min {Plugin.GroundBrakeMinSpeed:F0}), brake {inp.brake:F2}, throttle {inp.throttle:F2} (< {Plugin.GroundBrakeThrottle:F2}), " + string.Format("rotating {0}, blend {1:F2}, airbrake {2:F2} (split {3})", rotating, groundBrake, airBrake, SplitDeploy ? "out" : "stowed"));
			}
			float num18 = num / (Plugin.StickSmoothing + num);
			spF += (Mathf.Clamp(0f - inp.pitch, -1f, 1f) - spF) * num18;
			srF += (Mathf.Clamp(inp.roll, -1f, 1f) - srF) * num18;
			syF += (Mathf.Clamp(inp.yaw, -1f, 1f) - syF) * num18;
			sp = Mathf.MoveTowards(sp, spF, 8f * num);
			sr = Mathf.MoveTowards(sr, srF, RollStickRate * num);
			sy = Mathf.MoveTowards(sy, syF, 8f * num);
			airBrake = Mathf.MoveTowards(airBrake, (Mathf.Clamp01(inp.brake) > 0.05f) ? 1f : 0f, 2f * num);
			GroundSteer(num, flag4, flag5);
			Vector3 vector11;
			tvc.anchorK = 0f;
			if (Mode == FcsMode.Ground)
			{
				float num19 = 0f - sp;
				float num20 = sr;
				if (gsActive && gsMainsContact)
				{
					num20 = Mathf.Lerp(Mathf.Clamp((0f - phi) / GsPhiFull, -1f, 1f), sr, Mathf.Clamp01(Mathf.Abs(sr) / 0.3f));
				}
				float num21 = (gsActive ? gsRud : sy);
				float target = 0f;
				bool flag7 = flag4;
				bool flag8 = inp.brake > 0.05f || (Plugin.GroundBrakeThrottle >= 0f && inp.throttle < Plugin.GroundBrakeThrottle);
				if (Plugin.GroundAeroBrake > 0f && flag7 && flag8 && tas >= Plugin.GroundBrakeMinSpeed)
				{
					target = Plugin.GroundAeroBrake * airBrake;
				}
				groundBrake = Mathf.MoveTowards(groundBrake, target, 1.5f * num);
				if (brakeBias == null || brakeBias.Length != surfaces.Count)
				{
					brakeBias = new float[surfaces.Count];
				}
				for (int k = 0; k < surfaces.Count; k++)
				{
					brakeBias[k] = 0f;
				}
				if (groundBrake > 0.01f)
				{
					for (int l = 0; l < surfaces.Count; l++)
					{
						SurfaceEffector surfaceEffector = surfaces[l];
						if (surfaceEffector.active && surfaceEffector.kind != SurfaceKind.Rudder)
						{
							float teUp = surfaceEffector.teUp;
							if (teUp != 0f)
							{
								brakeBias[l] = groundBrake * teUp * ((surfaceEffector.kind == SurfaceKind.Stabilator) ? Plugin.GroundBrakeStab : Plugin.GroundBrakeFlap);
							}
						}
					}
					for (int m = 0; m < surfaces.Count; m++)
					{
						SurfaceEffector surfaceEffector2 = surfaces[m];
						if (surfaceEffector2.kind == SurfaceKind.Rudder && surfaceEffector2.partner is SurfaceEffector surfaceEffector3 && surfaceEffector2.active && surfaceEffector3.active)
						{
							int num22 = surfaces.IndexOf(surfaceEffector3);
							if (num22 >= 0)
							{
								float num23 = groundBrake * Plugin.GroundBrakeRudder;
								brakeBias[m] = surfaceEffector2.pairSign * num23;
								brakeBias[num22] = (0f - surfaceEffector3.pairSign) * num23;
							}
						}
					}
				}
				for (int n = 0; n < surfaces.Count; n++)
				{
					SurfaceEffector surfaceEffector4 = surfaces[n];
					surfaceEffector4.cmd = Mathf.Clamp(num19 * surfaceEffector4.origPitchRange + num20 * surfaceEffector4.origRollRange + num21 * surfaceEffector4.origYawRange + brakeBias[n], surfaceEffector4.min, surfaceEffector4.max);
				}
				tvc.cmd = Mathf.Clamp((0f - num19) * tvc.max + groundBrake * Plugin.GroundBrakeTvc, tvc.min, tvc.max);
				qCmd = (qT = omega.y * 57.29578f);
				qTprev = qT;
				qTraj.Reset(qT);
				pTraj.Reset(0f);
				pCmd = omega.x * 57.29578f;
				rCmd = omega.z * 57.29578f;
				omegaDotDes = Vector3.zero;
				unload = (prot = false);
				pS = 0f;
				gInt = 0f;
				aInt = 0f;
				vector11 = vector8;
				tvcPitchInput = Mathf.Clamp(num19, -1f, 1f);
			}
			else
			{
				PitchLaw(num, vc);
				LateralLaw(num);
				if (gsActive && gsMainsContact)
				{
					// rolling on the mains with the nose up (rotation / landing flare-out): the ground yaw-rate command replaces
					// the airborne sideslip law, so a crosswind cannot weathervane the jet off the line
					rCmd = gsRCmd;
					rDotFF = 0f;
					// on the mains the roll axis is a bank hold in the air law; a crabbed touchdown's tyre side force (2 m below
					// the CG) tips the jet onto one main and the neutral stick would then hold that bank. Level the wings instead,
					// fading out with roll stick.
					pCmd = Mathf.Lerp(Mathf.Clamp((0f - GsKphi) * phi, -30f, 30f), pCmd, Mathf.Clamp01(Mathf.Abs(sr) / 0.3f));
				}
				qTprev = qT;
				Vector3 vector12 = new Vector3(pCmd, qT, rCmd) * (MathF.PI / 180f) - omega;
				omegaDotDes = new Vector3(Plugin.KRoll * vector12.x + pDotFF * (MathF.PI / 180f), Plugin.KPitch * vector12.y + 0.5f * qTraj.v * (MathF.PI / 180f), Plugin.KYaw * vector12.z + rDotFF * (MathF.PI / 180f));
				float mDotY = (mLeadInit ? ((vector8.y - mPrevY) / num) : 0f);
				mPrevY = vector8.y;
				mLeadInit = true;
				mDotYF = mDotYF + (mDotY - mDotYF) * (num / (MLeadTau + num));
				vector11 = vector8 + MulI(omegaDotDes - vector7);
				vector11.y -= Plugin.HybridIndiGain * hybridDM;
				if (Plugin.ReleasedMomentLead > 0f)
				{
					float leadGate = (1f - Mathf.Clamp01((Mathf.Abs(sp) - 0.03f) / 0.15f)) * (1f - Mathf.Clamp01(Mathf.Abs(qTraj.v) / 20f)) * ((Mathf.Abs(qT) < 1f && !prot && !unload) ? 1f : 0f) * stabPitchFrac;
					mLeadGate = Mathf.MoveTowards(mLeadGate, leadGate, num / 0.2f);
					float leadM = Plugin.ReleasedMomentLead * mLeadGate * mDotYF * Mathf.Clamp01(1f - Plugin.HybridIndiGain); // 1.9.0: the hybrid INDI term covers this lag
					float leadErr = vector12.y * 57.29578f;
					mLeadAgree = ((LeadAgreeGate && leadM * leadErr < 0f) ? (1f - Mathf.Clamp01((Mathf.Abs(leadErr) - 1f) / 2f)) : 1f);
					vector11.y += leadM * mLeadAgree;
				}
				Vector3 vector13 = new Vector3(pCmd, qT, rCmd) * (MathF.PI / 180f);
				vector11 += Plugin.CouplingFF * (Vector3.Cross(vector13, MulI(vector13)) - Vector3.Cross(omega, MulI(omega)));
				vector11 += asymFF;
				tvc.lam = Mathf.Lerp(2E-05f, Mathf.Max(2E-05f, Plugin.TvcMoveCost), stabPitchFrac);
				TvcBudgetStep(num);
				float num24 = Mathf.Clamp01(Mathf.Abs(pCmd) / 40f) * stabPitchFrac * (1f - Mathf.InverseLerp(12f, 20f, Mathf.Abs(alpha))) * (1f - Mathf.Clamp01(Mathf.Abs(sp) / 0.15f)) * (1f - Mathf.Clamp01(Mathf.Abs(qT) / 8f));
				float num25 = ((WashK >= 0f) ? WashK : Plugin.RollNozzleHold);
				if (num24 < 0.05f)
				{
					tvcTrimRef += (tvc.cmdOut - tvcTrimRef) * (num / (0.3f + num));
				}
				tvc.lam = Mathf.Max(tvc.lam, 0.05f * Mathf.Clamp01(num25) * num24 * Mathf.InverseLerp(125f, 150f, ias));
				if (num25 > 0f && num24 > 0.02f)
				{
					for (int num26 = 0; num26 < effs.Count; num26++)
					{
						cmdSave[num26] = effs[num26].cmd;
					}
					alloc.Solve(effs, vector11);
					float b = ((WashTau >= 0f) ? WashTau : Plugin.RollNozzleWashout);
					tvcSlow += (tvc.cmd - tvcTrimRef - tvcSlow) * (num / (Mathf.Max(0.02f, b) + num));
					float num27 = 0f;
					foreach (SurfaceEffector surface5 in surfaces)
					{
						if (surface5.kind == SurfaceKind.Stabilator && surface5.active)
						{
							num27 = Mathf.Max(num27, Mathf.Abs(surface5.cmd) / Mathf.Max(1f, surface5.LimitFor(surface5.cmd)));
						}
					}
					float num28 = Mathf.Clamp01((WashUtilHi - num27) / 0.2f);
					float washTarget = Mathf.Clamp01(num25) * num24 * num28 * tvcSlow;
					washApplied = ((Plugin.RollNozzleWashoutSlew > 0f) ? Mathf.MoveTowards(washApplied, washTarget, Plugin.RollNozzleWashoutSlew * num) : washTarget);
					float hi = Mathf.Clamp(tvc.cmd - washApplied, tvc.lo, tvc.hi);
					for (int num29 = 0; num29 < effs.Count; num29++)
					{
						effs[num29].cmd = cmdSave[num29];
					}
					float lo = tvc.lo;
					float hi2 = tvc.hi;
					tvc.lo = (tvc.hi = hi);
					alloc.Solve(effs, vector11);
					tvc.lo = lo;
					tvc.hi = hi2;
					tvcPin = hi;
					tvcPinned = true;
				}
				else
				{
					tvcSlow += (0f - tvcSlow) * (num / (0.2f + num));
					washApplied = ((Plugin.RollNozzleWashoutSlew > 0f) ? Mathf.MoveTowards(washApplied, 0f, Plugin.RollNozzleWashoutSlew * num) : 0f);
					tvcPinned = false;
					alloc.Solve(effs, vector11);
				}
				stabBranch = 0;
				bool branchGate = Plugin.StabBranchSwitch && Plugin.StabBranchMaxAoA > 0f;
				if (branchGate)
				{
					float aAbs = Mathf.Abs(alpha);
					branchGateOn = branchGateOn ? (aAbs <= Plugin.StabBranchMaxAoA + 2f) : (aAbs <= Plugin.StabBranchMaxAoA);
					branchGate = branchGateOn;
				}
				foreach (SurfaceEffector sb in surfaces)
				{
					if (sb.kind != SurfaceKind.Stabilator)
					{
						continue;
					}
					sb.branchCool = Mathf.Max(0f, sb.branchCool - num);
					if (Mathf.Max(Mathf.Abs(sr), Mathf.Abs(sy)) < 0.1f)
					{
						sb.branchCool = Mathf.Min(sb.branchCool, BranchCoolTimeStop);
					}
					if (!branchGate || !sb.active)
					{
						sb.branchOn = false;
						continue;
					}
					if (!sb.branchOn && sb.branchCool > 0f)
					{
						continue;
					}
					if (Mathf.Abs(sb.LocalAoAAt(sb.cmd)) < sb.stallAoA + 10f)
					{
						continue;
					}
					float bestD = sb.cmd;
					float bestE = float.MaxValue;
					for (int gi = 0; gi < sb.N; gi++)
					{
						float gd = sb.grid[gi];
						if (gd < sb.lo - 0.0001f || gd > sb.hi + 0.0001f)
						{
							continue;
						}
						float la = Mathf.Abs(sb.LocalAoAAt(gd));
						if (la >= sb.stallAoA)
						{
							continue;
						}
						float e = Mathf.Abs(la - Plugin.StabBranchTarget);
						if (e < bestE)
						{
							bestE = e;
							bestD = gd;
						}
					}
					if (bestE == float.MaxValue)
					{
						sb.branchOn = false;
						continue;
					}
					float y0 = 0f;
					for (int ci = 0; ci < effs.Count; ci++)
					{
						cmdSave[ci] = effs[ci].cmd;
						if (effs[ci].active)
						{
							y0 += effs[ci].MAt(effs[ci].cmd).y;
						}
					}
					float slo = sb.lo;
					float shi = sb.hi;
					sb.lo = (sb.hi = (sb.cmd = bestD));
					alloc.Solve(effs, vector11);
					sb.lo = slo;
					sb.hi = shi;
					float y1 = 0f;
					for (int ci2 = 0; ci2 < effs.Count; ci2++)
					{
						if (effs[ci2].active)
						{
							y1 += effs[ci2].MAt(effs[ci2].cmd).y;
						}
					}
					float kP = IaInv[1, 1] * 57.29578f;
					bool pitchOk = Mathf.Abs(y1 - vector11.y) * kP <= Mathf.Abs(y0 - vector11.y) * kP + Plugin.StabBranchPitchTol;
					bool tvcRoom = tvc == null || !tvc.active || Mathf.Abs(tvc.cmd) <= 0.8f * Mathf.Max(1f, tvc.max);
					if (sb.branchOn)
					{
						sb.branchFail = (pitchOk ? 0f : (sb.branchFail + num));
						if (sb.branchFail > BranchFailTime)
						{
							for (int ci3 = 0; ci3 < effs.Count; ci3++)
							{
								effs[ci3].cmd = cmdSave[ci3];
							}
							sb.branchOn = false;
							sb.branchCool = ((Mathf.Max(Mathf.Abs(sr), Mathf.Abs(sy)) < 0.1f) ? BranchCoolTimeStop : BranchCoolTime);
						}
						else
						{
							stabBranch++;
						}
					}
					else if (pitchOk && tvcRoom)
					{
						sb.branchOn = true;
						sb.branchFail = 0f;
						stabBranch++;
					}
					else
					{
						for (int ci4 = 0; ci4 < effs.Count; ci4++)
						{
							effs[ci4].cmd = cmdSave[ci4];
						}
						sb.branchCool = ((Mathf.Max(Mathf.Abs(sr), Mathf.Abs(sy)) < 0.1f) ? BranchCoolTimeStop : BranchCoolTime);
					}
				}
				rollPitchFF = 0f;
				float num30 = Plugin.RollPitchFF * (1f - Mathf.Clamp01((Mathf.Abs(alpha) - (Plugin.RollPitchFFAoA - 15f)) / 15f));
				if (num30 > 0.01f)
				{
					foreach (SurfaceEffector surface6 in surfaces)
					{
						if (surface6.active && surface6.noPitch)
						{
							float c = Mathf.MoveTowards(surface6.now, surface6.cmd, Mathf.Max(1f, surface6.rate) * num);
							rollPitchFF += surface6.PitchRawAt(c) - surface6.PitchRawAt(surface6.now);
						}
					}
					rollPitchFF *= num30;
					float num31 = Plugin.RollPitchFFMax / Mathf.Max(IaInv[1, 1] * 57.29578f, 1E-06f);
					rollPitchFF = Mathf.Clamp(rollPitchFF, 0f - num31, num31);
					if (Mathf.Abs(rollPitchFF) * IaInv[1, 1] * 57.29578f > 0.5f)
					{
						float lam = tvc.lam;
						int num32 = 0;
						if (Plugin.RollPitchFFStabCost > 1.01f)
						{
							tvc.lam = 2E-05f;
							foreach (SurfaceEffector surface7 in surfaces)
							{
								if (surface7.kind == SurfaceKind.Stabilator && surface7.partner != null && num32 < symSave.Length)
								{
									symSave[num32] = surface7.symCost;
									surface7.symCost *= Plugin.RollPitchFFStabCost;
									num32++;
								}
							}
						}
						float lo2 = tvc.lo;
						float hi3 = tvc.hi;
						if (tvcPinned)
						{
							tvc.lo = (tvc.hi = tvcPin);
						}
						int num33 = 0;
						foreach (SurfaceEffector surface8 in surfaces)
						{
							if (surface8.active && surface8.noPitch && num33 < pinLo.Length)
							{
								pinLo[num33] = surface8.lo;
								pinHi[num33] = surface8.hi;
								surface8.lo = (surface8.hi = Mathf.Clamp(surface8.cmd, surface8.lo, surface8.hi));
								num33++;
							}
						}
						alloc.Solve(effs, vector11 - new Vector3(0f, rollPitchFF, 0f));
						num33 = 0;
						foreach (SurfaceEffector surface9 in surfaces)
						{
							if (surface9.active && surface9.noPitch && num33 < pinLo.Length)
							{
								surface9.lo = pinLo[num33];
								surface9.hi = pinHi[num33];
								num33++;
							}
						}
						tvc.lo = lo2;
						tvc.hi = hi3;
						tvc.lam = lam;
						if (Plugin.RollPitchFFStabCost > 1.01f)
						{
							num32 = 0;
							foreach (SurfaceEffector surface10 in surfaces)
							{
								if (surface10.kind == SurfaceKind.Stabilator && surface10.partner != null && num32 < symSave.Length)
								{
									surface10.symCost = symSave[num32];
									num32++;
								}
							}
						}
					}
				}
				RollPitchPass(vector11, num);
				Vector3 zero4 = Vector3.zero;
				dbgYawRud = (dbgYawWing = (dbgYawStab = (dbgYawTvc = 0f)));
				foreach (Effector eff3 in effs)
				{
					if (!eff3.active)
					{
						continue;
					}
					Vector3 vector14 = eff3.MAt(eff3.cmd);
					zero4 += vector14;
					if (DebugYaw)
					{
						float num34 = vector14.z * IaInv[2, 2] * 57.29578f;
						if (eff3.name.Contains("Rudder"))
						{
							dbgYawRud += num34;
						}
						else if (eff3.name.Contains("Flap") || eff3.name.Contains("Aileron"))
						{
							dbgYawWing += num34;
						}
						else if (eff3.name.Contains("Elevator"))
						{
							dbgYawStab += num34;
						}
						else
						{
							dbgYawTvc += num34;
						}
					}
				}
				dbgTargetZ = vector11.z * IaInv[2, 2] * 57.29578f;
				dbgMfZ = vector8.z * IaInv[2, 2] * 57.29578f;
				dbgTgtY = vector11.y * IaInv[1, 1] * 57.29578f;
				dbgMfY = vector8.y * IaInv[1, 1] * 57.29578f;
				float num35 = (vector11.z - zero4.z) * Mathf.Sign(vector11.z - vector8.z);
				yawSat = yawSatF.Step(Mathf.Clamp01((num35 * IaInv[2, 2] * 57.29578f - 10f) / 40f), 0.25f, num);
				float num36 = 10f;
				float num37 = 1f;
				if (tvc.engines.Count > 0 && tvc.engines[0] != null)
				{
					num36 = Mathf.Max(0.1f, Mathf.Abs(Acc.TF_vec(tvc.engines[0]).x));
					float x4 = Acc.TF_vecGain(tvc.engines[0]).x;
					num37 = ((Mathf.Abs(x4) > 0.001f) ? x4 : 1f);
				}
				tvcPitchInput = Mathf.Clamp((0f - tvc.cmdOut) / (num36 * num37), -1f, 1f);
			}
			mTarget = vector11;
			float actuatorDamping = Plugin.ActuatorDamping;
			foreach (Effector eff4 in effs)
			{
				if (Mode == FcsMode.Ground || actuatorDamping <= 0.0001f)
				{
					eff4.cmdOut = eff4.cmd;
					eff4.cmdLag = eff4.cmd;
					eff4.outTraj.Reset(eff4.cmd);
				}
				else if (LegacyLag)
				{
					eff4.cmdOut += (eff4.cmd - eff4.cmdOut) * (num / (actuatorDamping + num));
					eff4.outTraj.Reset(eff4.cmdOut);
				}
				else
				{
					float num38 = Mathf.Max(1f, eff4.rate);
					eff4.cmdLag += (eff4.cmd - eff4.cmdLag) * (num / (PreLag + num));
					eff4.outTraj.Step(eff4.cmdLag, num38, num38 / actuatorDamping, num);
					eff4.cmdOut = eff4.outTraj.x;
				}
			}
			if (Mode == FcsMode.Ground)
			{
				float num39 = 10f;
				float num40 = 1f;
				if (tvc.engines.Count > 0 && tvc.engines[0] != null)
				{
					num39 = Mathf.Max(0.1f, Mathf.Abs(Acc.TF_vec(tvc.engines[0]).x));
					float x5 = Acc.TF_vecGain(tvc.engines[0]).x;
					num40 = ((Mathf.Abs(x5) > 0.001f) ? x5 : 1f);
				}
				float num41 = groundBrake * Plugin.GroundBrakeTvc / (num39 * num40);
				tvcPitchInput = Mathf.Clamp(0f - sp - num41, -1f, 1f);
			}
			foreach (SurfaceEffector surface11 in surfaces)
			{
				if (surface11.active)
				{
					surface11.localAoACmd = surface11.LocalAoAAt(surface11.cmd);
				}
			}
			Engaged = true;
			if (isAI && Plugin.AIFlightLog)
			{
				AILogStep(num);
			}
			if (!telemetryOpened && Plugin.TelemetryEnabled && !isAI)
			{
				telemetryOpened = true;
				telemetry = new Telemetry(this);
			}
			telemetry?.Write(time, sp, sr, sy, num2);
		}

		private float RateBudget()
		{
			float num = Mathf.Clamp(5f + 0.15f * ias + 40f * tw, 5f, Plugin.MaxPitchRate);
			float b = Mathf.Clamp((ias - 22f) * 1.1f, 0f, Plugin.MaxPitchRate);
			float t = Mathf.Clamp01((Mathf.Abs(alpha) - (Plugin.CoupleAoA - 6f)) / 12f);
			return Mathf.Clamp(Mathf.Lerp(Mathf.Max(num, b), num, t), 6f, Plugin.MaxPitchRate);
		}

		private float AIStickRate()
		{
			float num = Mathf.Max(1f, aiCornerSpeed);
			float num2 = tas * tas * Mathf.Max(0.01f, ac.airDensity) / (num * num * 1.225f);
			float num3 = aiGLimit * 9.81f / Mathf.Max(tas, 0.75f * num) * 57.29578f;
			if (num2 < 1f)
			{
				num3 *= Mathf.Clamp(num2, 0.3f, 1f);
			}
			return num3;
		}

		// 1.8.40: roll-pitch priority. While a roll is commanded (and for RpRelease s after), the flaperons' pitch moment
		// is in the allocator's model instead of being fed forward: the roll's pitch side-effect is then balanced in the
		// same solve that picks the roll deflections, with pitch weighted ~8-15x roll, so the solver takes it off the roll
		// (less flaperon/stab differential) before it pushes the stabs past their lift peak or the nozzles off their trim.
		// The flaperon pair's collective costs RpSymCost x more meanwhile, so they are not used as pitch surfaces.
		internal static float RpEnable = 1f;

		internal static float RpSymCost = 50f;

		internal static float RpRelease = 0.5f;

		internal static float RpAoA0 = 18f;

		internal static float RpAoA1 = 26f;

		private float rpLat;

		public float rpGate;

		internal static float RpPitchW = 10f;

		internal static float RpPitchTol = 4f;

		internal static float RpYawW = 10f;

		internal static float RpErrBand = 0f;

		internal static float RpHpTau = 0.4f;

		internal static float RpHpK = 0f;

		private float rpTvcLp;

		private bool rpLpInit;

		internal static float RpErr0 = 1.5f;

		internal static float RpBandMin = 0.5f;

		public float rpBand = -1f;

		public float rpPitchErr;

		// After the normal allocation: if the nozzles were sent more than RollTvcBudget off their pre-roll trim while a
		// roll is commanded, solve again with the nozzles held inside that band, every surface's true pitch curve
		// (flaperon pitch moment in, stab lift past its peak honest) and pitch weighted RpPitchW x more. The roll is what
		// gives. Accepted only if its modelled pitch error stays within RpPitchTol deg/s^2 of the normal solution's;
		// otherwise the band is widened (x3, x8) and finally the normal solution kept.
		private void RollPitchPass(Vector3 target, float dt)
		{
			rpBand = -1f;
			rpPitchErr = 0f;
			if (tvc == null || !tvc.active)
			{
				return;
			}
			// the band is centred on the pre-roll trim plus the fast (high-passed) part of what the normal solution wants
			// from the nozzles: they keep doing the quick pitch corrections the surfaces cannot follow (reversals, actuator
			// lag) and hand the steady part of the roll's pitch moment to the stabs within RpHpTau.
			float dtR = Mathf.Max(0.001f, dt);
			float tvcMain = tvc.cmd;
			if (!rpLpInit || rpGate <= 0.01f)
			{
				rpTvcLp = tvcMain;
				rpLpInit = true;
			}
			else
			{
				rpTvcLp += (tvcMain - rpTvcLp) * (dtR / (Mathf.Max(0.01f, RpHpTau) + dtR));
			}
			if (rpGate <= 0.01f)
			{
				return;
			}
			float center = tvcAnchor + RpHpK * (tvcMain - rpTvcLp);
			// the band opens with the pitch-rate error: the nozzles stay the fast pitch effector for transients the
			// surfaces cannot follow (roll reversals: rate limits and actuator lag), only the steady roll trim is held off them
			float qErrNow = Mathf.Abs(omega.y * 57.29578f - qT);
			float band0 = Mathf.Max(RpBandMin, Plugin.RollTvcBudget) + (1f - rpGate) * (1f - rpGate) * Mathf.Max(tvc.max, 0f - tvc.min) * RpGateSpan + RpErrBand * Mathf.Max(0f, qErrNow - RpErr0);
			// with afterburner-class thrust the nozzles are the stronger and faster pitch effector: holding them off the
			// trim moves the stabs' reserve for reversal transients into the trim (bench: asym-load AB roll spam overshot
			// the AoA cap by 3-4 deg more). The band widens by RpTwBand between T/W 0.7 and 1.1.
			band0 += RpTwBand * Mathf.Clamp01((tw - TvcBudgetTw0) / Mathf.Max(0.05f, TvcBudgetTwFade));
			// roll stop / reversal (stick against the roll rate): the roll's pitch moment is about to swing the other way
			// faster than the nozzles slew, so they are let go for it
			float pNow = omega.x * 57.29578f;
			float rev = Mathf.Clamp01(-Mathf.Sign(pNow) * sr * 2f) * Mathf.Clamp01((Mathf.Abs(pNow) - 20f) / 40f);
			band0 += RpRevBand * rev;
			rpRevNow = Mathf.Max(rev, rpRevNow - dtR / Mathf.Max(0.05f, RpRevHold));
			// AoA protection / overshoot past the AoA (or asymmetric-load) limit: the nozzles are free again
			band0 += Mathf.Max(prot ? 1f : 0f, pitchNeed) * Mathf.Max(tvc.max, 0f - tvc.min) * 2f;
			float dev = tvc.cmd - center;
			if (Mathf.Abs(dev) <= band0 + 0.05f)
			{
				return;
			}
			int n = effs.Count;
			if (cmdSave.Length < n)
			{
				cmdSave = new float[n * 2];
			}
			for (int i = 0; i < n; i++)
			{
				cmdSave[i] = effs[i].cmd;
			}
			float kP = IaInv[1, 1] * 57.29578f;
			int ns = 0;
			int nl = 0;
			foreach (SurfaceEffector se in surfaces)
			{
				// stabs stay on the attached side of their lift peak in this pass: past it, their pitch moment turns with the
				// roll (the pitch-up this pass exists to avoid) and lags the plan by a stall's worth of travel
				if (RpStallLimit && se.kind == SurfaceKind.Stabilator && se.active && nl < pinLo.Length)
				{
					pinLo[nl] = se.lo;
					pinHi[nl] = se.hi;
					nl++;
					StallWindow(se, se.stallAoA + RpStallMargin);
					// and keep a reserve short of the travel stops for the pitch transients to come (a roll reversal
					// swings the roll's pitch moment the other way faster than the nozzles slew)
					float rlo = se.min + RpStabReserve;
					float rhi = se.max - RpStabReserve;
					if (se.lo < rlo)
					{
						se.lo = Mathf.Min(rlo, se.hi);
					}
					if (se.hi > rhi)
					{
						se.hi = Mathf.Max(rhi, se.lo);
					}
				}
				se.HonestPitchOn();
				if (se.noPitch && se.partner != null && se.kind != SurfaceKind.Rudder && ns < symSave.Length)
				{
					symSave[ns++] = se.symCost;
					se.symCost *= 1f + RpSymCost;
				}
			}
			Vector3 w0 = alloc.axisWeight;
			alloc.axisWeight = new Vector3(w0.x, w0.y * RpPitchW, w0.z * RpYawW);
			float lo0 = tvc.lo;
			float hi0 = tvc.hi;
			// reference: the same solve with the nozzles free - the pitch error the surfaces cannot avoid anyway
			alloc.Solve(effs, target);
			float eA = Mathf.Abs(PitchSum() - target.y) * kP;
			bool ok = false;
			float e = 0f;
			for (int k = 0; k < 3 && !ok; k++)
			{
				float band = band0 * ((k == 0) ? 1f : ((k == 1) ? 3f : 8f));
				for (int i = 0; i < n; i++)
				{
					effs[i].cmd = cmdSave[i];
				}
				float lo = Mathf.Max(lo0, center - band);
				float hi = Mathf.Min(hi0, center + band);
				if (lo > hi)
				{
					lo = (hi = ((center - band > hi0) ? hi0 : lo0));
				}
				tvc.lo = lo;
				tvc.hi = hi;
				alloc.Solve(effs, target);
				e = Mathf.Abs(PitchSum() - target.y) * kP;
				if (e <= eA + RpPitchTol + RpRevTol * rpRevNow)
				{
					ok = true;
					rpBand = band;
				}
			}
			tvc.lo = lo0;
			tvc.hi = hi0;
			alloc.axisWeight = w0;
			ns = 0;
			nl = 0;
			foreach (SurfaceEffector se2 in surfaces)
			{
				if (RpStallLimit && se2.kind == SurfaceKind.Stabilator && se2.active && nl < pinLo.Length)
				{
					se2.lo = pinLo[nl];
					se2.hi = pinHi[nl];
					nl++;
				}
				se2.HonestPitchOff();
				if (se2.noPitch && se2.partner != null && se2.kind != SurfaceKind.Rudder && ns < symSave.Length)
				{
					se2.symCost = symSave[ns++];
				}
			}
			rpPitchErr = e;
			if (!ok)
			{
				for (int i = 0; i < n; i++)
				{
					effs[i].cmd = cmdSave[i];
				}
			}
		}

		internal static bool RpStallLimit = true;

		internal static float RpStallMargin = 0f;

		internal static float RpStabReserve = 0f;

		internal static float RpTwBand = 0f;

		// 1.8.42: on roll stops/reversals the nozzles are no longer let go (RpRevBand 12 -> 0); the pass is instead accepted
		// with up to RpRevTol deg/s^2 more modelled pitch error than free nozzles would leave, so the stabs and roll rate
		// take the reversal's pitch swing and the nozzles stay near trim (2026-09-28 21:18 log: -20 deg and +1.7..3.3 deg
		// over the 12 deg cap on reversals at 600 km/h).
		internal static float RpRevBand = 12f;

		internal static float RpRevTol = 0f;

		internal static float RpRevHold = 0.6f;

		internal static float HonestRollGate = 0f;

		private float rpRevNow;

		internal static float RpGateSpan = 1f;

		internal static bool RpMainStall = false;

		internal static float RpMainAoA = 18f;

		internal static float RpMainStallMargin = 3f;

		internal static bool StabWindowAlways = false;

		// narrow lo..hi to the deflections whose local AoA stays within +-lim (keeps the side the surface is on if none do)
		private static void StallWindow(SurfaceEffector se, float lim)
		{
			float a = float.MaxValue;
			float b = float.MinValue;
			for (int i = 0; i < se.N; i++)
			{
				float g = se.grid[i];
				if (g < se.lo - 0.0001f || g > se.hi + 0.0001f)
				{
					continue;
				}
				if (Mathf.Abs(se.LocalAoAAt(g)) <= lim)
				{
					a = Mathf.Min(a, g);
					b = Mathf.Max(b, g);
				}
			}
			if (a > b)
			{
				return;
			}
			float h = 0.5f * se.Step;
			float lo = Mathf.Max(se.lo, a - h);
			float hi = Mathf.Min(se.hi, b + h);
			if (lo > hi)
			{
				// the attached window is out of this frame's reach: go toward it as far as the surface can
				lo = (hi = ((b + h < se.lo) ? se.lo : se.hi));
			}
			se.lo = lo;
			se.hi = hi;
		}

		private float PitchSum()
		{
			float y = 0f;
			foreach (Effector ef in effs)
			{
				if (ef.active)
				{
					y += ef.MAt(ef.cmd).y;
				}
			}
			return y;
		}

		private void RollPitchGateStep(float dt)
		{
			float g = (Plugin.RollPitchPriority && Engaged && Mode != FcsMode.Ground && Mode != FcsMode.Off) ? RpEnable : 0f;
			float latNow = Mathf.Max(Mathf.Clamp01((Mathf.Max(Mathf.Abs(sr), Mathf.Abs(sy)) - 0.02f) / 0.2f), Mathf.Clamp01(Mathf.Abs(pCmd) / 30f));
			rpLat = Mathf.Max(latNow, rpLat - dt / Mathf.Max(0.05f, RpRelease));
			g *= rpLat * (1f - Mathf.Clamp01((Mathf.Abs(alpha) - RpAoA0) / Mathf.Max(1f, RpAoA1 - RpAoA0)));
			rpGate = Mathf.Clamp01(g);
		}

		// 1.8.32: during roll/yaw the nozzles keep within RollTvcBudget of where they were before the lateral input
		// (the pitch trim they held for the pilot's own pitch command); the pitch moment the roll itself makes is
		// taken by the stabilators beyond that. A cost in the allocator rather than a clamp: when the stabs cannot
		// supply it (on a stop, stalled) the nozzles still go past the budget, as far as the pitch error needs.
		// The band opens to full travel between 14 and 20 deg AoA, between T/W 0.7 and 1.1, in AoA protection / run-away
		// and while the pitch-rate command is moving (AoA/G capture braking).
		private void TvcBudgetStep(float dt)
		{
			tvc.anchorK = 0f;
			float budget = Plugin.RollTvcBudget;
			if (!tvcAnchorInit)
			{
				tvcAnchor = tvc.cmd;
				tvcAnchorInit = true;
			}
			float latNow = Mathf.Max(Mathf.Clamp01((Mathf.Max(Mathf.Abs(sr), Mathf.Abs(sy)) - 0.02f) / 0.2f), Mathf.Clamp01(Mathf.Abs(pCmd) / 30f));
			tvcBudgetLat = Mathf.Max(latNow, tvcBudgetLat - dt / Mathf.Max(0.05f, TvcBudgetRelease));
			// Where the stabs can take the pitch (g = 1) the band is RollTvcBudget; it opens toward the full nozzle travel
			// as they lose that ability or the nozzles need the room: AoA from 14 to 20 deg (the stab going trailing-edge
			// down for the roll is then at ~30 deg local AoA, where the F22E_Wing foil's lift curve flattens, and nose-down from it costs roll rate:
			// 13-22 % in 250 km/h roll spam on the bench), AoA protection / run-away, and while the pitch-rate command is
			// moving (AoA/G capture braking keeps full authority).
			float g = 1f;
			g *= 1f - Mathf.Clamp01((Mathf.Abs(alpha) - TvcBudgetAoA) / Mathf.Max(1f, TvcBudgetAoAFade));
			g *= prot ? 0f : (1f - pitchNeed);
			g *= 1f - Mathf.Clamp01(Mathf.Abs(qTraj.v) / TvcBudgetQdot);
			g *= stabPitchFrac;
			// With afterburner-class thrust the nozzles' roll trim is only a few degrees anyway (1.8.31 bench: -2..-7 mean)
			// and they are the fast pitch effector; holding them there moved that fast work onto the stabs, which rang
			// with 3 frames of control delay (450 km/h AB roll spam: q sd 2.1 -> 3.8 deg/s). Full budget up to T/W 0.7,
			// open by 1.1.
			g *= 1f - Mathf.Clamp01((tw - TvcBudgetTw0) / Mathf.Max(0.05f, TvcBudgetTwFade));
			float active = tvcBudgetLat;
			if (budget <= 0f || Plugin.RollTvcBudgetCost <= 0f)
			{
				active = 0f;
			}
			tvcBudgetGate = active * g;
			// the anchor (pre-roll nozzle trim for the pilot's own pitch command) follows the nozzles only while no lateral input is held
			float follow = 1f - Mathf.Clamp01(active / 0.3f);
			tvcAnchor += (tvc.cmd - tvcAnchor) * follow * (dt / (TvcAnchorTau + dt));
			if (active <= 0.001f)
			{
				return;
			}
			float span = Mathf.Max(tvc.max, 0f - tvc.min);
			tvc.anchor = tvcAnchor;
			tvc.band = Mathf.Lerp(span * 2f, budget, active * g);
			tvc.anchorK = Plugin.RollTvcBudgetCost;
		}

		internal static float TvcBudgetRelease = 0.5f;

		internal static float TvcBudgetAoA = 14f;

		internal static float TvcBudgetAoAFade = 6f;

		internal static float TvcBudgetQdot = 25f;

		internal static float TvcBudgetTw0 = 0.7f;

		internal static float TvcBudgetTwFade = 0.4f;

		internal static float TvcAnchorTau = 0.25f;

		// 1.9.0: highest AoA at which the nose-down authority available now (stabs full nose-down, flaperons where they
		// are, nozzles at the present thrust - none above the vectoring cut-off speed) still leaves LowThrustCapMargin.
		private void AuthCapFromSweep()
		{
			if (!Plugin.LowThrustAoACap || Ia[1, 1] <= 1f)
			{
				authCap = 999f;
				return;
			}
			float tvDn = 0f;
			if (tvc.active && tvc.M != null)
			{
				float tvLo = float.MaxValue;
				for (int tk = 0; tk < tvc.N; tk++)
				{
					tvLo = Mathf.Min(tvLo, tvc.M[tk].y);
				}
				if (tvLo < float.MaxValue)
				{
					tvDn = Mathf.Min(0f, tvLo * IaInv[1, 1] * 57.29578f);
				}
			}
			// 1.9.5: nozzle nose-down authority growing (throttle up / spool-up): the cap may follow at once
			if (0f - tvDn > authTvPrev + AuthCapThrustStep)
			{
				authFastT = AuthCapFastHold;
			}
			authTvPrev = 0f - tvDn;
			float margin = Plugin.AuthCapMargin;
			float cap = 999f;
			float prevA = 0f;
			float prevM = float.NaN;
			for (int ia = 0; ia < asymAlphas.Length; ia++)
			{
				float aA = asymAlphas[ia];
				if (aA < 0f)
				{
					continue;
				}
				float mDn = 0f - (authNetDn[ia] + tvDn);
				if (mDn < margin)
				{
					float aP = (float.IsNaN(prevM) || prevM <= mDn) ? aA : Mathf.Lerp(prevA, aA, (prevM - margin) / (prevM - mDn));
					cap = Mathf.Max(Plugin.AuthCapFloor, aP - AuthCapLead);
					break;
				}
				prevA = aA;
				prevM = mDn;
			}
			authCap = cap;
		}

		// 1.9.0: the flaperons' symmetric deflection for the low-thrust cap, followed only while no roll/yaw is commanded:
		// during a roll one flaperon runs into its stop and the pair's mean shifts, which is not a trim the cap should judge by
		private float[] flapSymRef;

		private bool flapSymInit;

		private void FlapSymStep(float dt)
		{
			float[] anti = asymDsym;
			if (anti == null || anti.Length != surfaces.Count)
			{
				return;
			}
			if (flapSymRef == null || flapSymRef.Length != surfaces.Count)
			{
				flapSymRef = new float[surfaces.Count];
				flapSymInit = false;
			}
			bool quiet = Mathf.Max(Mathf.Abs(sr), Mathf.Abs(sy)) < 0.05f && Mathf.Abs(omega.x * 57.29578f) < 15f;
			float k = Mathf.Clamp01(dt / 0.3f);
			for (int i = 0; i < surfaces.Count; i++)
			{
				SurfaceEffector se = surfaces[i];
				float sym = se.now - anti[i];
				if (!flapSymInit)
				{
					flapSymRef[i] = sym;
				}
				else if (quiet)
				{
					flapSymRef[i] += (sym - flapSymRef[i]) * k;
				}
			}
			flapSymInit = true;
		}

		private void AuthCapStep(float dt)
		{
			if (!Plugin.LowThrustAoACap)
			{
				authCapEff = 999f;
				authInit = false;
				return;
			}
			float tgt = Mathf.Min(authCap, Plugin.AoAPos);
			if (!authInit)
			{
				authCapEff = tgt;
				authInit = true;
				return;
			}
			if (tgt < authCapEff)
			{
				authCapEff += (tgt - authCapEff) * Mathf.Clamp01(dt / AuthCapTauDown);
				return;
			}
			// 1.9.1: rising, the cap is also rate-limited, and nearly frozen while the jet sits at or above it. In 1.9.0 the
			// exponential rise (up to ~10 deg/s just after a drop) let the capture re-accelerate the nose into the still-
			// rising cap and the recovery push it back: a 1 Hz, 2-18 deg/s pitch-rate cycle at 55-60 deg AoA, half thrust.
			authFastT = Mathf.Max(0f, authFastT - dt);
			if (authFastT > 0f)
			{
				// 1.9.5: the rise comes from more thrust (the nozzles' nose-down authority grew at the last sweeps), not from
				// the airframe - follow it as fast as a drop. 1.9.1-1.9.4 held it to 1 deg/s while the jet sat at the cap, so
				// idle -> max AB with the stick held took ~40 s to give back the full AoA.
				authCapEff += (tgt - authCapEff) * Mathf.Clamp01(dt / AuthCapTauDown);
				return;
			}
			float riseMax = ((alpha > authCapEff - 1f) ? AuthCapRiseAtCap : AuthCapRise) * dt;
			authCapEff += Mathf.Min((tgt - authCapEff) * Mathf.Clamp01(dt / AuthCapTauUp), riseMax);
		}

		private static float SmoothMin(float a, float b, float k)
		{
			float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / Mathf.Max(0.01f, k));
			return Mathf.Lerp(b, a, h) - k * h * (1f - h);
		}

		private static float Smooth01(float x)
		{
			x = Mathf.Clamp01(x);
			return x * x * (3f - 2f * x);
		}

		// 1.9.0: AoA/G command pre-filter. The reference moves toward the stick's command at a speed that grows with the
		// step (small steps gently, large ones as before), its rate building up over an onset time that also shrinks with
		// the step. Released stick: the reference sits on the jet's present AoA/G, so the next step is measured from there.
		private static float ShapeStep(ref float x, ref float v, ref float mag, float target, float span, float kSmall, float kLarge, float onsetSmall, float dt)
		{
			float d = target - x;
			// the step's size is held (decaying at one span per second) so a large step keeps its speed all the way in
			// instead of slowing down on its last few degrees / tenths of a g as if it were a small one
			mag = Mathf.Max(Mathf.Abs(d), mag - span * dt);
			float r = Mathf.Clamp01(mag / span);
			float s = r * r;
			float k = Mathf.Lerp(kSmall, kLarge, s);
			float ton = Mathf.Lerp(onsetSmall, 0.03f, s);
			float vDes = k * d;
			if (vDes * v < 0f)
			{
				v = 0f;
			}
			float aLim = Mathf.Max(Mathf.Abs(vDes) / Mathf.Max(0.01f, ton), k * k * Mathf.Abs(d));
			v = Mathf.MoveTowards(v, vDes, aLim * dt);
			float xn = x + v * dt;
			if ((target - xn) * d <= 0f)
			{
				xn = target;
				v = 0f;
			}
			x = xn;
			return x;
		}

		private void ShapeCommands(float dt, bool held)
		{
			if (!Plugin.CmdShaping)
			{
				alphaCmd = alphaCmdRaw;
				nCmd = nCmdRaw;
				shapeInit = false;
				return;
			}
			if (!held || !shapeInit)
			{
				aRefX = alpha;
				nRefX = nz;
				aRefV = 0f;
				nRefV = 0f;
				aRefMag = 0f;
				nRefMag = 0f;
				shapeInit = true;
			}
			if (held)
			{
				ShapeStep(ref aRefX, ref aRefV, ref aRefMag, alphaCmdRaw, Plugin.ShapeAoASpan, Plugin.ShapeAoAGainSmall, Plugin.ShapeAoAGainLarge, Plugin.ShapeOnsetSmall, dt);
				ShapeStep(ref nRefX, ref nRefV, ref nRefMag, nCmdRaw, Plugin.ShapeGSpan, Plugin.ShapeGGainSmall, Plugin.ShapeGGainLarge, Plugin.ShapeOnsetSmall, dt);
				alphaCmd = aRefX;
				nCmd = nRefX;
			}
			else
			{
				alphaCmd = alphaCmdRaw;
				nCmd = nCmdRaw;
			}
		}

		private float aRefX;

		private float nRefX = 1f;

		private float aRefMag;

		private float nRefMag;

		private void PitchLaw(float dt, float Vc)
		{
			qB = RateBudget();
			AuthCapStep(dt);
			latReserve = Mathf.Clamp01(Plugin.LateralStabReserve) * Mathf.Clamp01(Mathf.Max(Mathf.Abs(sr), Mathf.Abs(sy))) * Mathf.Clamp01((Mathf.Abs(sp) - 0.03f) / 0.15f);
			float aUpP = aUp;
			float aDnP = aDn;
			if (latReserve > 0.001f)
			{
				float tvUp = 0f;
				float tvDn = 0f;
				if (tvc.active && tvc.M != null && tvc.N > 1)
				{
					float tvMax = float.MinValue;
					float tvMin = float.MaxValue;
					for (int tvI = 0; tvI < tvc.N; tvI++)
					{
						tvMax = Mathf.Max(tvMax, tvc.M[tvI].y);
						tvMin = Mathf.Min(tvMin, tvc.M[tvI].y);
					}
					tvUp = Mathf.Max(0f, (tvMax - tvc.Mnow.y) * IaInv[1, 1] * 57.29578f);
					tvDn = Mathf.Max(0f, (tvc.Mnow.y - tvMin) * IaInv[1, 1] * 57.29578f);
				}
				aUpP = Mathf.Max(20f, aUp - latReserve * Mathf.Max(0f, aUp - tvUp));
				aDnP = Mathf.Max(20f, aDn - latReserve * Mathf.Max(0f, aDn - tvDn));
			}
			aUpPitch = aUpP;
			aDnPitch = aDnP;
			bool flag = Mode != FcsMode.MPO;
			float num = (flag ? Plugin.AoAPos : 999f);
			float num2 = (flag ? Plugin.AoANeg : (-999f));
			float num3 = (flag ? Mathf.Min(Mathf.Min(num, asymCapPosEff), authCapEff) : num);
			float num4 = (flag ? Mathf.Max(num2, asymCapNegEff) : num2);
			float num5 = ((Mode == FcsMode.MPO) ? Plugin.MpoGPos : Plugin.GPos);
			float num6 = ((Mode == FcsMode.MPO) ? Plugin.MpoGNeg : Plugin.GNeg);
			float num7 = 9.81f / Vc * 57.29578f;
			float num8 = (dbgWVe = omega.y * 57.29578f - alphaDot);
			alphaDotHoldG = Mathf.Clamp(2f * Mathf.Abs(nz) * Mathf.Max(0f, 0f - vDot) / (Vc * Mathf.Max(nAlpha, 0.02f)), 0f, 15f);
			unload = false;
			prot = false;
			float num9 = 1f;
			float num10 = 0f;
			alphaCmd = 0f;
			nCmd = 1f;
			betaG = 0f;
			if (Mode == FcsMode.AoAG)
			{
				// 1.9.0: hand-over AoA (lower when the wings can barely lift the weight), the stick breakpoint that keeps the
				// AoA scale below it, and how far into the rate-command travel the stick is
				float aTgtRb = Plugin.RateBlendAoA;
				if (nLiftMax < Plugin.RateBlendLiftG)
				{
					aTgtRb = Mathf.Lerp(Mathf.Min(Plugin.RateBlendAoAMin, Plugin.RateBlendAoA), Plugin.RateBlendAoA, Mathf.Clamp01((nLiftMax - 1f) / (Plugin.RateBlendLiftG - 1f)));
				}
				// 1.9.7: the negative side's threshold (magnitude) from the wings' negative-lift capacity
				float aTgtRbN = Plugin.RateBlendAoANeg;
				if (nLiftMin < Plugin.RateBlendLiftG)
				{
					aTgtRbN = Mathf.Lerp(Mathf.Min(Plugin.RateBlendAoAMinNeg, Plugin.RateBlendAoANeg), Plugin.RateBlendAoANeg, Mathf.Clamp01((nLiftMin - 1f) / (Plugin.RateBlendLiftG - 1f)));
				}
				rateBlendAoA = (rbInit ? (rateBlendAoA + (aTgtRb - rateBlendAoA) * Mathf.Clamp01(dt / 0.5f)) : aTgtRb);
				rateBlendAoANeg = (rbInit ? (rateBlendAoANeg + (aTgtRbN - rateBlendAoANeg) * Mathf.Clamp01(dt / 0.5f)) : aTgtRbN);
				rbInit = true;
				// 1.9.1 stick map (fixed AoA per stick: RateBlendAoA at RateBlendStick, 72 deg per full stick by default).
				// The threshold's stick point moves with the threshold (24 deg -> 0.333). Up to it: AoA command on that line.
				// Past it: AoA command on from the threshold to the envelope limit at full stick (full stick = the limit, so
				// the jet goes through the threshold at the rate budget); once the jet crosses the threshold with the stick
				// there, pitch-rate command. With the envelope limit (asymmetric / low-thrust cap) at or below the
				// threshold the whole travel maps onto the limit as before; blended over the 4 deg above it.
				// 1.9.7: the same on the negative side, mirrored. Everything below runs in side space (s = +1 pull, -1 push):
				// stick s*sp, AoA s*alpha, limit s*limit, threshold as a magnitude; the outputs are mapped back with s.
				bool blendP = Plugin.HighAoARateBlend;
				bool blendN = Plugin.HighAoARateBlendNeg;
				float aTP = rateBlendAoA;
				float aTN = rateBlendAoANeg;
				float limP = num3;
				float limN = 0f - num4;
				float gAP = Plugin.RateBlendAoA / Plugin.RateBlendStick;
				float gAN = Plugin.RateBlendAoANeg / Plugin.RateBlendStick;
				float zwP = (blendP ? Mathf.Clamp01((limP - aTP) / 4f) : 0f);
				float zwN = (blendN ? Mathf.Clamp01((limN - aTN) / 4f) : 0f);
				float aBP = (blendP ? Mathf.Min(aTP, limP) : limP); // 1.9.5: with the blend off full stick is the AoA limit again (1.9.1-1.9.4 stopped at the threshold)
				float aBN = (blendN ? Mathf.Min(aTN, limN) : limN);
				float sBP = Mathf.Lerp(1f, Mathf.Clamp(aTP / gAP, 0.05f, 0.95f), zwP);
				float sBN = Mathf.Lerp(1f, Mathf.Clamp(aTN / gAN, 0.05f, 0.95f), zwN);
				// floating hold point: the jet came back down to the threshold out of rate command with the stick past the
				// threshold's point - that stick position is the threshold now; easing below it holds the (live) threshold
				// with a soft knee until the stick meets the threshold's own point on the line, where it is caught
				if (floatOn)
				{
					float fsp = (float)floatSide * sp;
					bool fzone = ((floatSide > 0) ? zwP : zwN) > 0.001f;
					if (!fzone || fsp <= ((floatSide > 0) ? sBP : sBN) || Mathf.Abs(sp) < 0.03f || fsp > 0.98f)
					{
						floatOn = false;
					}
				}
				int sm = ((sp >= 0f) ? 1 : (-1));
				float spS = (float)sm * sp;
				float aT = ((sm > 0) ? aTP : aTN);
				float zw = ((sm > 0) ? zwP : zwN);
				bool zone = zw > 0.001f;
				float aB = ((sm > 0) ? aBP : aBN);
				float sB = ((sm > 0) ? sBP : sBN);
				float limS = ((sm > 0) ? limP : limN);
				float sH = ((floatOn && floatSide == sm) ? Mathf.Max(floatS, sB) : sB);
				float aCmdS;
				if (spS <= sH || sH > 0.999f)
				{
					aCmdS = aB * Mathf.Min(spS, 1f) / Mathf.Max(sB, 0.01f);
					if (floatOn && floatSide == sm)
					{
						aCmdS = SmoothMin(aCmdS, aT, FloatKnee);
					}
					aCmdS = Mathf.Min(aCmdS, limS);
				}
				else
				{
					aCmdS = aB + (spS - sH) / (1f - sH) * Mathf.Max(0f, limS - aB);
				}
				alphaCmdRaw = (float)sm * aCmdS;
				nCmdRaw = ((sp >= 0f) ? (1f + sp * (num5 - 1f)) : (1f + sp * (1f - num6)));
				zoneX = ((zone && spS > sH) ? Mathf.Clamp01((spS - sH) / Mathf.Max(0.02f, 1f - sH)) : 0f);
				num9 = Mathf.Clamp01((Mathf.Abs(sp) - 0.01f) / 0.06f);
				ShapeCommands(dt, num9 > 0.5f);
				float a = 1f + qB * (MathF.PI / 180f) * Vc / 9.81f;
				gAvail = Mathf.Min(a, nMaxAero);
				betaG = Mathf.Clamp01((gAvail - num5) / 3f + 0.5f);
				if (!hiRate)
				{
					if (zone && zoneX > 0f && (float)sm * alpha >= aT && betaG < 0.5f && num9 > 0.5f)
					{
						hiRate = true;
						hiSide = sm;
						hiAnchor = true;
						hiAS = spS;
						hiAQ = Mathf.Clamp((float)sm * qTraj.x, 0f, qB);
						floatOn = false;
					}
				}
				else
				{
					// 1.9.5: only a pilot ease latches it - not a full pull (full stick always means all the way), and not the
					// jet being pushed down through the threshold by an envelope cap coming down (throttle to idle)
					float hs = hiSide;
					float aTh = ((hiSide > 0) ? aTP : aTN);
					float sBh = ((hiSide > 0) ? sBP : sBN);
					bool zoneh = ((hiSide > 0) ? zwP : zwN) > 0.001f;
					bool capPush = ((hiSide > 0) ? (num3 < num - 0.05f && alphaU > num3 - 1f) : (num4 > num2 + 0.05f && alphaU < num4 + 1f));
					if (hs * alpha < aTh && hs * sp > sBh && hs * sp < 0.95f && !capPush && !floatOn && zoneh)
					{
						floatOn = true;
						floatS = hs * sp;
						floatSide = hiSide;
					}
					if (!zoneh || hs * alpha < aTh - Plugin.RateBlendBand || betaG > 0.6f)
					{
						hiRate = false;
					}
				}
				bool flag2 = !hiRate && sp * alpha >= 0f && Mathf.Abs(alpha) > 3f && Mathf.Abs(alphaCmd) < Mathf.Abs(alpha) - 1f;
				bool flag3 = sp * (nz - 1f) >= 0f && Mathf.Abs(nCmd - 1f) < Mathf.Abs(nz - 1f) - 0.3f;
				unload = ((betaG < 0.5f) ? flag2 : flag3);
				if (nz > num5 || nz < num6 || alpha > num || alpha < num2 || num9 < 0.5f)
				{
					unload = false;
				}
				num10 = ((betaG < 0.5f) ? Mathf.Sign(alpha) : Mathf.Sign(nz - 1f));
				float num11 = alphaCmd - alpha;
				float num12 = Mathf.Sign(num11) * Mathf.Min(Plugin.KpAoA * Mathf.Abs(num11), Mathf.Sqrt(240f * Mathf.Abs(num11)));
				if (betaG < 0.5f && num9 > 0.5f && !flag2 && !hiRate && Mathf.Abs(num11) < 4f && Mathf.Abs(qT) < 0.95f * qB)
				{
					aInt = Mathf.Clamp(aInt + Plugin.KiAoA * num11 * dt, -10f, 10f);
				}
				else
				{
					aInt = Mathf.MoveTowards(aInt, 0f, 20f * dt);
				}
				float a2 = num8 + num12 + aInt;
				if (flag2)
				{
					float num13 = num8 + (NPath(alphaCmd) - NPath(alpha)) * num7;
					float num14 = Mathf.Max(0f, num10 * alphaDot);
					a2 = num13 + (0.15f + 0.45f * (1f - Mathf.Clamp01(Mathf.Abs(num11) / 10f))) * num11 - num10 * Plugin.UnloadRiseGain * num14;
				}
				rateW = 0f;
				if (hiRate)
				{
					// past the threshold: pitch-rate command. The map runs through (0, 0), the entry point (stick, rate at
					// the crossing) and (full stick, budget); a release drops the entry point (zero-rate hold, and the next
					// pull maps 0..full stick straight onto 0..budget).
					// a release or a full pull drops the entry point: from then on 0..full stick = 0..budget
					// 1.9.7: in side space (hiSide), so a push past the negative threshold is the same nose-down rate command
					float hs = hiSide;
					float spH = hs * sp;
					if (Mathf.Abs(sp) < 0.03f || spH > 0.98f)
					{
						hiAnchor = false;
					}
					float qMap;
					if (spH <= 0f)
					{
						qMap = spH * qB;
					}
					else if (hiAnchor && hiAS > 0.05f)
					{
						qMap = ((spH <= hiAS || hiAS > 0.999f) ? (hiAQ * spH / hiAS) : Mathf.Lerp(hiAQ, Mathf.Max(hiAQ, qB), (spH - hiAS) / (1f - hiAS)));
					}
					else
					{
						qMap = spH * qB;
					}
					qMap = Mathf.Min(qMap, qB);
					float aTh = ((hiSide > 0) ? aTP : aTN);
					rateW = Smooth01((hs * alpha - (aTh - Plugin.RateBlendBand)) / Plugin.RateBlendBand);
					a2 = Mathf.Lerp(a2, hs * qMap, rateW);
				}
				float num15 = Mathf.Max(nAlpha, 0.1f);
				float b;
				if (flag3)
				{
					float num16 = Mathf.Max(0f, num10 * alphaDot);
					b = (nCmd - eLy) * num7 + 0.3f * Plugin.KpG * (nCmd - nz) / num15 - num10 * Plugin.UnloadRiseGain * num16;
				}
				else
				{
					b = (nCmd - eLy) * num7 + Plugin.KpG * (nCmd - nz) / num15 + gInt + Mathf.Sign(nCmd - 1f) * alphaDotHoldG;
				}
				float num17 = Mathf.Lerp(a2, b, betaG);
				if (betaG > 0.5f && num9 > 0.5f && !unload && Mathf.Abs(qT) < 0.95f * qB && Mathf.Abs(nCmd - nz) < 1.5f)
				{
					gInt = Mathf.Clamp(gInt + Plugin.KiG * (nCmd - nz) / Mathf.Max(nAlpha, 0.1f) * dt, -10f, 10f);
				}
				else
				{
					gInt = Mathf.MoveTowards(gInt, 0f, 10f * dt);
				}
				qRaw = num17 * num9;
			}
			else
			{
				qRaw = sp * (isAI ? Mathf.Min(qB, AIStickRate()) : qB);
				hiRate = false;
				floatOn = false;
				shapeInit = false;
				rateW = 0f;
				zoneX = 0f;
				if (rotating && mainsOnGround)
				{
					qRaw = sp * Mathf.Min(qB, Plugin.RotationRate);
				}
				gInt = 0f;
				aInt = 0f;
			}
			float num18 = qB;
			float num19 = 0f - qB;
			if (flag)
			{
				float num20 = num3;
				float num21 = num4;
				if (Mode == FcsMode.AoAG && !unload && num9 > 0.5f && betaG < 0.5f && !hiRate)
				{
					if (alphaCmdRaw > 5f)
					{
						num20 = Mathf.Min(num3, alphaCmdRaw + 0.2f);
					}
					if (alphaCmdRaw < -5f)
					{
						num21 = Mathf.Max(num4, alphaCmdRaw - 0.2f);
					}
				}
				float num22 = Mathf.Max(0f, (num20 - alpha) * 0.9f - Mathf.Max(0f, alphaDot) * 0.15f);
				float num23 = Mathf.Max(0f, (alpha - num21) * 0.9f - Mathf.Max(0f, 0f - alphaDot) * 0.15f);
				aBrakeTgt = aDn;
				if (CaptureMode == 1 && Plugin.AoACaptureThrustBrake && asymValid && tvc.M != null && tvc.N > 1)
				{
					float tvLo = float.MaxValue;
					float tvHi = float.MinValue;
					for (int tvJ = 0; tvJ < tvc.N; tvJ++)
					{
						tvLo = Mathf.Min(tvLo, tvc.M[tvJ].y);
						tvHi = Mathf.Max(tvHi, tvc.M[tvJ].y);
					}
					float tvLoA = (tvc.active ? (tvLo * IaInv[1, 1] * 57.29578f) : 0f);
					float tvHiA = (tvc.active ? (tvHi * IaInv[1, 1] * 57.29578f) : 0f);
					float brDn = 0f - (AsymCap.Interp(asymAlphas, asymNetDn, asymAlphas.Length, num20) + tvLoA);
					float brUp = AsymCap.Interp(asymAlphas, asymNetUp, asymAlphas.Length, num21) + tvHiA;
					aBrakeTgt = Mathf.Max(20f, Mathf.Min(aDn, brDn));
					float aBrakeUp = Mathf.Max(20f, Mathf.Min(aUp, brUp));
					aBrakeTgt += Mathf.Clamp01(Plugin.AoACaptureBrakeBlend) * Mathf.Max(0f, aDn - aBrakeTgt);
					aBrakeUp += Mathf.Clamp01(Plugin.AoACaptureBrakeBlend) * Mathf.Max(0f, aUp - aBrakeUp);
					num18 = Mathf.Min(num18, num8 + Mathf.Min(Mathf.Sqrt(1.4f * aBrakeTgt * num22), 3f * num22));
					num19 = Mathf.Max(num19, num8 - Mathf.Min(Mathf.Sqrt(1.4f * aBrakeUp * num23), 3f * num23));
				}
				else
				{
					num18 = Mathf.Min(num18, num8 + Mathf.Min(Mathf.Sqrt(2f * aDn * num22), 3f * num22));
					num19 = Mathf.Max(num19, num8 - Mathf.Min(Mathf.Sqrt(2f * aUp * num23), 3f * num23));
				}
			}
			if (tas > 25f)
			{
				float num24 = Mathf.Max(nAlpha, 0.02f);
				float num25 = ((nz > 1f) ? alphaDotHoldG : 0f);
				float num26 = ((nz < 0f) ? alphaDotHoldG : 0f);
				float num27 = 0.15f + RollLoadLead * Mathf.Clamp01(Mathf.Abs(omega.x * 57.29578f) / 150f);
				float num28 = (num5 - nz) / num24 - Mathf.Max(0f, alphaDot - num25) * num27;
				float num29 = (nz - num6) / num24 - Mathf.Max(0f, 0f - alphaDot - num26) * num27;
				float num30 = ((num28 > 0f) ? Mathf.Min(Mathf.Sqrt(2f * aDn * num28), 3f * num28) : (3f * num28));
				float num31 = ((num29 > 0f) ? Mathf.Min(Mathf.Sqrt(2f * aUp * num29), 3f * num29) : (3f * num29));
				num18 = Mathf.Min(num18, num8 + num25 + num30);
				num19 = Mathf.Max(num19, num8 - num26 - num31);
				// 1.9.0: only while the load is being taken off (the stick eased, or the shaped G command still coming down to
				// it). In a steady hold its 0.15 g dead zone pinned the load 0.15-0.2 g above the command once the hybrid
				// pitch loop removed the oscillation that used to hide it.
				bool gUnderGate = unload || nCmdRaw < nCmd - 0.05f || !Plugin.CmdShaping;
				if (Mode == FcsMode.AoAG && betaG > 0.5f && Plugin.GUndershootCapture > 0f && gUnderGate)
				{
					float num32 = (nz - nCmd - 0.15f) / num24;
					float num33 = ((num32 > 0f) ? Mathf.Min(Mathf.Sqrt(2f * aUp * num32), 3f * num32) : (3f * num32));
					if (num32 > 0f || !GUnderAboveOnly)
					{
						num19 = Mathf.Max(num19, num8 - num33 / Mathf.Clamp01(Plugin.GUndershootCapture));
					}
					gUnderDbg = num33;
				}
				else
				{
					gUnderDbg = 0f;
				}
			}
			if (protHold && (!flag || Mode != FcsMode.AoAG || Mathf.Abs(sp) < 0.7f || (protHoldSide > 0f && alphaU < num - 5f) || (protHoldSide < 0f && alphaU > num2 + 5f)))
			{
				protHold = false;
			}
			if (flag && Mode == FcsMode.AoAG && !protHold)
			{
				if (alphaU > num + 5f && sp > 0.85f)
				{
					protHold = true;
					protHoldSide = 1f;
				}
				else if (alphaU < num2 - 5f && sp < -0.85f)
				{
					protHold = true;
					protHoldSide = -1f;
				}
			}
			float num34;
			// 1.9.5: the pilot's own command within the normal bounds - the AoA protection may not hold it back from a
			// command that is already beyond protection's own (more nose-down above the limit, more nose-up below the
			// negative one). Before, protection set both bounds to its command, and from 1.9.3 (soft onset) that command is
			// ~the flight-path rate just past the limit: full forward stick at 65.5 deg still got +8..+12 deg/s nose-up.
			float qPilot = ((!(num19 > num18)) ? Mathf.Clamp(qRaw, num19, num18) : ((alpha >= 0f) ? num18 : num19));
			if (protHold)
			{
				num34 = 0f;
				num18 = (num19 = num34);
				prot = true;
			}
			else if (flag && alphaU > num + 0.5f)
			{
				// 1.9.3: continuous onset. The protection branch starts at alpha > limit + 0.5, where the capture branch commands
				// the flight-path rate; it used to subtract sqrt(2 a e) + 4 there at once (~14 deg/s), a step that pitched the
				// jet 3-4.5 deg below the limit in max-AoA turns (the 'pitch hitch'). Now 0 at the threshold, linear
				// (ProtOnsetGain /s per deg) until it meets the braking curve, and the +4 ramps in over 2 deg.
				float eP = alphaU - num - 0.5f;
				float num35 = Mathf.Min(Plugin.ProtRate, Mathf.Min(ProtOnsetGain * eP, Mathf.Sqrt(2f * Mathf.Max(aUp, 40f) * (alphaU - num))) + 4f * Mathf.Clamp01(eP / 2f));
				float num36 = Mathf.Max(num8 - num35, 0f - Plugin.ProtRate);
				float num37 = 1f - Mathf.Clamp01(sp / 0.9f);
				num34 = ((num36 < 0f) ? (num36 * num37) : num36);
				num34 = Mathf.Min(num34, qPilot);
				num18 = (num19 = num34);
				// 1.9.3: the protection state (pitch priority in the allocator, fight costs off, ...) only past ProtFlagBand,
				// so a jet holding the limit in a turn does not toggle it every few frames
				prot = eP > ProtFlagBand;
			}
			else if (flag && alphaU < num2 - 0.5f)
			{
				float eN = num2 - alphaU - 0.5f;
				float num38 = Mathf.Min(Plugin.ProtRate, Mathf.Min(ProtOnsetGain * eN, Mathf.Sqrt(2f * Mathf.Max(aDn, 40f) * (num2 - alphaU))) + 4f * Mathf.Clamp01(eN / 2f));
				float num39 = Mathf.Min(num8 + num38, Plugin.ProtRate);
				float num40 = 1f - Mathf.Clamp01((0f - sp) / 0.9f);
				num34 = ((num39 > 0f) ? (num39 * num40) : num39);
				num34 = Mathf.Max(num34, qPilot);
				num18 = (num19 = num34);
				prot = eN > ProtFlagBand;
			}
			else
			{
				num34 = ((!(num19 > num18)) ? Mathf.Clamp(qRaw, num19, num18) : ((alpha >= 0f) ? num18 : num19));
				if (unload)
				{
					if (num10 > 0f)
					{
						num34 = Mathf.Max(num34, 0f);
					}
					else if (num10 < 0f)
					{
						num34 = Mathf.Min(num34, 0f);
					}
				}
				if (Mode == FcsMode.AoAG)
				{
					if (sp > 0.02f && alpha > -2f)
					{
						num34 = Mathf.Max(num34, (nz > num5) ? ((0f - Plugin.ProtRate) * (1f - sp)) : 0f);
					}
					else if (sp < -0.02f && alpha < 2f)
					{
						num34 = Mathf.Min(num34, (nz < num6) ? (Plugin.ProtRate * (1f + sp)) : 0f);
					}
				}
			}
			if (flag && !prot)
			{
				if (num3 < num - 0.05f && alphaU > num3)
				{
					float num41 = alphaU - num3;
					float num42 = Mathf.Min(Plugin.AsymRecoveryRate, Mathf.Min(3f * num41, Mathf.Sqrt(2f * Mathf.Max(aUp, 40f) * num41)));
					num34 = Mathf.Min(num34, Mathf.Max(num8 - num42, 0f - Plugin.ProtRate));
				}
				else if (num4 > num2 + 0.05f && alphaU < num4)
				{
					float num43 = num4 - alphaU;
					float num44 = Mathf.Min(Plugin.AsymRecoveryRate, Mathf.Min(3f * num43, Mathf.Sqrt(2f * Mathf.Max(aDn, 40f) * num43)));
					num34 = Mathf.Max(num34, Mathf.Min(num8 + num44, Plugin.ProtRate));
				}
			}
			qMaxDbg = num18;
			qMinDbg = num19;
			qCmd = Mathf.Clamp(num34, 0f - Plugin.ProtRate, Plugin.ProtRate);
			if (Mode == FcsMode.AoAG && !prot && Plugin.PitchAntiWindup > 0f && num9 > 0.5f)
			{
				float aw = Plugin.PitchAntiWindup * (qCmd - qRaw) * dt;
				if (aw * aInt < 0f)
				{
					aInt = ((aInt > 0f) ? Mathf.Max(0f, aInt + (1f - betaG) * aw) : Mathf.Min(0f, aInt + (1f - betaG) * aw));
				}
				if (aw * gInt < 0f)
				{
					gInt = ((gInt > 0f) ? Mathf.Max(0f, gInt + betaG * aw) : Mathf.Min(0f, gInt + betaG * aw));
				}
			}
			float num45 = qCmd - qTraj.x;
			bool pitchOnset = num45 * qTraj.x >= 0f;
			float a3 = Mathf.Max(0.7f * ((num45 > 0f) ? (pitchOnset ? aUpP : aUp) : (pitchOnset ? aDnP : aDn)), 30f);
			a3 = Mathf.Min(a3, Mathf.Max(30f, Plugin.PitchOnsetGps / Mathf.Max(0.05f, nAlpha)));
			// 1.9.0: stick released in the G regime: the pitch rate is taken off over ~ReleaseTauG instead of in one step, so
			// the load comes off at a steadier rate at high dynamic pressure (the zero-rate hold itself is unchanged)
			if (Mode == FcsMode.AoAG && Plugin.CmdShaping && !prot && betaG > 0.5f && Mathf.Abs(sp) < 0.07f && num45 * qTraj.x < 0f)
			{
				a3 = Mathf.Min(a3, Mathf.Max(3f, Mathf.Abs(qTraj.x) / ReleaseTauG));
			}
			float j = (prot ? (a3 / 0.1f) : (a3 / 0.15f));
			if (unload)
			{
				a3 = ((num10 * alphaDot > 0.5f) ? Mathf.Min(a3, Mathf.Max(60f, 2f * Plugin.UnloadDecel)) : Mathf.Min(a3, Plugin.UnloadDecel));
				j = a3 / 0.3f;
			}
			qTraj.Step(qCmd, a3, j, dt);
			qT = qTraj.x;
		}

		internal static float LowIasYawBoostAt(float iasNow)
		{
			if (YawBoostFixed >= 1f)
			{
				return YawBoostFixed;
			}
			float num = Mathf.Max(1f, Plugin.LowIasYawBoost) - 1f;
			float lowIasYawBoostFullIas = Plugin.LowIasYawBoostFullIas;
			float num2 = ((iasNow <= lowIasYawBoostFullIas) ? 1f : ((float)Math.Exp((0f - (iasNow - lowIasYawBoostFullIas)) / Mathf.Max(1f, Plugin.LowIasYawBoostFalloff))));
			num2 *= Mathf.Clamp01((120f - iasNow) / 10f);
			return 1f + num * num2;
		}

		internal static float FightPedalRelease = 1f;

		internal static int FightMode = 0;


		internal static float PitchFightQdot = 25f;

		internal static float PitchFightQHold = 0.6f;

		private float pfQHold;

		public static float PitchFightErr0 = 3f;

		public static bool PitchFightAbsErr = false;

		public static float PitchFightErrSpan = 5f;

		internal static float FightRollRelease = 0.3f;

		private float fightRollHold;

		private float fightPed;

		// 1.8.41 pitch-reserve roll governor. Rolling at a held or restricted AoA makes pitch-up: the canted/stalled
		// down-going stab and flaperon lose lift for roll, and the inertial coupling (Izz-Ixx)/Iyy * p * r grows with the
		// roll rate squared. Above the roll-pitch-priority band (18-26 deg AoA) only the nozzles are left to hold it, and with
		// a store they sat on the -20 deg stop for whole rolls (2026-09-28 log: 28 deg cap, full aft + roll, nozzles pinned
		// 4 s). The governor watches the nozzles' remaining nose-down travel (nose-up at negative AoA) at the live thrust and,
		// while a roll is commanded, lowers the roll-rate limit and entry acceleration when less than RollPitchReserve of
		// their nose-down authority is left; it gives it back as the margin returns. Measured state only - no AoA/speed
		// schedule. 0 = off (1.8.40).
		internal static float RollGovDown = 1.5f;

		internal static float RollGovUp = 0.6f;

		internal static float RollGovFloor = 0.35f;

		internal static float RollGovMinRate = 30f;

		internal static float RollGovAccelFloor = 0.5f;

		internal static float RollGovAoA0 = 3f;

		public float rollGov = 1f;

		public float rollGovMargin;

		private Lpf rollGovMF;

		private void RollGovStep(float dt, float pLim, float rollSide)
		{
			float res = Plugin.RollPitchReserve;
			if (res <= 0f || tvc == null || !tvc.active || tvc.N < 2 || tvc.M == null || !Engaged || Mode == FcsMode.Ground || Mode == FcsMode.Off || revFlow)
			{
				rollGov = Mathf.MoveTowards(rollGov, 1f, 2f * dt);
				rollGovMargin = 1f;
				return;
			}
			float yLo = float.MaxValue;
			float yHi = float.MinValue;
			for (int k = 0; k < tvc.N; k++)
			{
				yLo = Mathf.Min(yLo, tvc.M[k].y);
				yHi = Mathf.Max(yHi, tvc.M[k].y);
			}
			float yCmd = tvc.MAt(tvc.cmd).y;
			float yMid = tvc.MAt(0f).y;
			// margin toward the side the roll's pitch moment pushes against: nose-down at positive AoA
			float half = (alpha >= 0f) ? (yMid - yLo) : (yHi - yMid);
			float marg = (alpha >= 0f) ? (yCmd - yLo) : (yHi - yCmd);
			float frac = (half > 1E-06f) ? Mathf.Clamp01(marg / half) : 1f;
			frac = rollGovMF.Step(frac, 0.06f, dt);
			rollGovMargin = frac;
			float aoaGate = Mathf.Clamp01((Mathf.Abs(alpha) - RollGovAoA0) / 3f);
			float latNow = Mathf.Max(Mathf.Clamp01((Mathf.Abs(sr) - 0.05f) / 0.2f), Mathf.Clamp01((Mathf.Abs(pTraj.x) - 10f) / 20f));
			if (latNow * aoaGate < 0.01f)
			{
				rollGov = Mathf.MoveTowards(rollGov, 1f, 2f * dt);
				return;
			}
			float need = Mathf.Clamp01(res);
			if (frac < need)
			{
				float deficit = (need - frac) / Mathf.Max(0.05f, need);
				rollGov = Mathf.Max(RollGovFloor, rollGov - RollGovDown * deficit * aoaGate * latNow * dt);
			}
			else
			{
				rollGov = Mathf.Min(1f, rollGov + RollGovUp * Mathf.Clamp01((frac - need) / Mathf.Max(0.05f, need)) * dt);
			}
		}

		private void LateralLaw(float dt)
		{
			float f = alpha * (MathF.PI / 180f);
			float num = Mathf.Cos(f);
			float num2 = Mathf.Sin(f);
			pFull = rollCtrlHalf / Mathf.Max(1f, 0f - Lp) * 57.29578f;
			pStockRef = StockRollRef();
			float trimR = ((RollTrimHeadroom && Plugin.AsymRollHeadroom) ? (Mathf.Sign(dTrimR) * Mathf.Min(Mathf.Max(0f, Mathf.Abs(dTrimR) - Mathf.Max(0f, Plugin.AsymDeadband)), 0.8f * aRoll)) : 0f);
			aRollPos = aRoll - Mathf.Max(0f, 0f - trimR);
			aRollNeg = aRoll - Mathf.Max(0f, trimR);
			float rollSide = sr + Mathf.Clamp01((Mathf.Abs(alpha) - Plugin.CoupleAoA) / 14f) * sy;
			if (Mathf.Abs(rollSide) < 0.02f)
			{
				rollSide = pTraj.x;
			}
			float aRollGo = ((rollSide >= 0f) ? aRollPos : aRollNeg);
			float aRollStop = ((rollSide >= 0f) ? aRollNeg : aRollPos);
			float num3 = Mathf.Min(Plugin.MaxRollRate, Mathf.Min(pStockRef, Plugin.RollAuthorityUse * pFull * (aRollGo / aRoll)));
			if (Plugin.RollStopFactor > 0.01f)
			{
				num3 = Mathf.Min(num3, Plugin.RollStopFactor * Mathf.Min(aRollGo, aRollStop));
			}
			float num4 = ((Mode == FcsMode.MPO) ? Plugin.MpoGPos : Plugin.GPos);
			float num5 = ((Mode == FcsMode.MPO) ? Plugin.MpoGNeg : Plugin.GNeg);
			float value = 1f;
			float t = Mathf.Clamp01((ias - 250f) / 150f);
			float num6 = Mathf.Lerp(6f, 3f, t);
			if (nz > num6)
			{
				value = Mathf.Lerp(1f, Mathf.Lerp(0.5f, 0.35f, t), (nz - num6) / Mathf.Max(0.5f, num4 - num6));
			}
			else if (nz < -1.5f)
			{
				value = Mathf.Lerp(1f, 0.6f, (-1.5f - nz) / Mathf.Max(0.5f, -1.5f - num5));
			}
			num3 *= Mathf.Clamp(value, 0.4f, 1f);
			num3 = (pMaxDbg = Mathf.Max(num3, 30f));
			float num7 = LowIasYawBoostAt(ias);
			float num8 = (rMaxDbg = Mathf.Lerp(Plugin.MaxYawRate, Mathf.Clamp(num7 * Plugin.YawStopFactor * aYaw, 20f * num7, Plugin.MaxYawRate), Mathf.Clamp01((Mathf.Abs(alpha) - 15f) / 10f)));
			// 1.8.45 braking floor: at high AoA the roll and yaw rate limits may not exceed what the stop-side authority,
			// at the IAS predicted from a rapid deceleration, brakes in the stop time of the reference manoeuvre (full
			// pedal + full lateral stick at 65 deg AoA, 280 km/h: RollStopFactor for roll, YawStopFactor x yaw boost there
			// for yaw). Replaces the fixed 30 deg/s roll / 20 x boost yaw floors there, which outran the authority below ~65 m/s.
			float gB = Mathf.Clamp01((Mathf.Abs(alpha) - LatBrakeA0) / Mathf.Max(1f, LatBrakeA1 - LatBrakeA0));
			latPredK = qPredK;
			if (LatBrakeFloor && gB > 0f)
			{
				// 1.9.3: yaw floor = the yaw braking at 270 km/h; from 210 to 150 km/h the allowed stop time grows smoothly
				// (smoothstep) by YawMarginLow (1.5 x: 50 % more yaw rate for the same authority, slower braking), flat below.
				// The roll stop time gets the same factor: at 65 deg AoA a velocity-vector turn needs body roll = cos(AoA) x its
				// rate, and a roll limit left at 1.0 s capped the turn (and so the yaw rate) at ~160 km/h.
				// The floor limits join the normal limits with a C1 soft minimum (no corner near 270 km/h).
				// Roll: the stop-side authority is the modelled asymmetric-load trim off the full authority, low-passed. The
				// measured trim (dTrimR) it used carried the turn's own dihedral / yaw-rate roll moment, which swings with
				// sideslip and yaw rate: in a max-AoA stick turn it moved 20 <-> 29 deg/s^2 at 1.4 Hz, the roll ceiling
				// 9 <-> 18 deg/s with it, the velocity-vector ceiling 20 <-> 42, and yaw rate 27 <-> 40 (a limit cycle).
				float iasKmh = ias * 3.6f;
				float sm = Mathf.Clamp01((YawMarginIasHi - iasKmh) / Mathf.Max(1f, YawMarginIasHi - YawMarginIasLo));
				sm = sm * sm * (3f - 2f * sm);
				float yawMargin = Mathf.Lerp(1f, Mathf.Max(1f, YawMarginLow), sm);
				yawMarginDbg = yawMargin;
				float tY = Plugin.YawStopFactor * LowIasYawBoostAt(BrakeRefIas) * yawMargin;
				float asymTrim = asymValid ? Mathf.Max(0f, Mathf.Abs(asymArNow) - Mathf.Max(0f, Plugin.AsymDeadband)) : 0f;
				float stopA = Mathf.Max(0.2f * aRoll, aRoll - Mathf.Min(asymTrim, 0.8f * aRoll));
				rollStopAF = (rollStopAF < 0f) ? stopA : (rollStopAF + (stopA - rollStopAF) * Mathf.Clamp01(dt / Mathf.Max(0.02f, RollStopTau)));
				float pB = Mathf.Max(LatBrakeMinRate, Mathf.Max(0.01f, Plugin.RollStopFactor) * yawMargin * rollStopAF * qPredK);
				float rB = Mathf.Max(LatBrakeMinRate, tY * aYaw * qPredK);
				num3 = Mathf.Lerp(num3, SoftMin(num3, pB, BrakeSoftMin * 0.5f * (num3 + pB)), gB);
				// 1.9.4: mid-IAS band (290-330 km/h) with less yaw stop time (x YawMidFactor), smoothstep ramps outside it
				float mLo = Mathf.Clamp01((iasKmh - (Plugin.YawMidLoKmh - Plugin.YawMidRampKmh)) / Mathf.Max(1f, Plugin.YawMidRampKmh));
				float mHi = Mathf.Clamp01(((Plugin.YawMidHiKmh + Plugin.YawMidRampKmh) - iasKmh) / Mathf.Max(1f, Plugin.YawMidRampKmh));
				mLo = mLo * mLo * (3f - 2f * mLo);
				mHi = mHi * mHi * (3f - 2f * mHi);
				float yawMid = Mathf.Lerp(1f, Plugin.YawMidFactor, Mathf.Min(mLo, mHi));
				yawMidDbg = yawMid;
				num8 = Mathf.Lerp(num8, yawMid * SoftMin(num8, rB, BrakeSoftMin * 0.5f * (num8 + rB)), gB);
				pMaxDbg = num3;
				rMaxDbg = num8;
			}
			float num9 = Mathf.Clamp01((Mathf.Abs(alpha) - Plugin.CoupleAoA) / 14f);
			float num10 = Mathf.Min(num3 / Mathf.Max(Mathf.Abs(num), 0.05f), num8 / Mathf.Max(Mathf.Abs(num2), 0.05f));
			float num11 = Mathf.Clamp(((revFlow && Plugin.RevFlowRollSense) ? (-1f) : 1f) * (sr + num9 * sy), -1f, 1f) * num10;
			coupRollLim = 0f;
			if (Plugin.CouplingRollLimit > 0f && Ia[1, 1] > 1f && asymValid && tvc.M != null && tvc.N > 1)
			{
				float kIc = (Ia[2, 2] - Ia[0, 0]) / Ia[1, 1];
				float s2c = Mathf.Abs(Mathf.Sin(2f * f));
				float gateC = Mathf.Max(pitchNeed, Mathf.Clamp01((Mathf.Sign(alpha) * alphaDot - 5f) / 15f)) * Mathf.Clamp01((Mathf.Abs(alpha) - CoupGateA0) / 10f);
				gateC = Mathf.Max(gateC, (Plugin.AsymRollCoupling ? CoupHeldGate : 0f) * Mathf.Clamp01((Mathf.Abs(alpha) - 4f) / 4f) * Mathf.Clamp01((Mathf.Abs(asymDy) - CoupHeldDy0) / CoupHeldDySpan));
				if (gateC > 0.001f && s2c > 0.05f && kIc > 0.01f)
				{
					float tvLoC = float.MaxValue;
					float tvHiC = float.MinValue;
					for (int tvK = 0; tvK < tvc.N; tvK++)
					{
						tvLoC = Mathf.Min(tvLoC, tvc.M[tvK].y);
						tvHiC = Mathf.Max(tvHiC, tvc.M[tvK].y);
					}
					float netC = ((alpha >= 0f) ? (0f - (AsymCap.Interp(asymAlphas, asymNetDn, asymAlphas.Length, alpha) + (tvc.active ? (tvLoC * IaInv[1, 1] * 57.29578f) : 0f))) : (AsymCap.Interp(asymAlphas, asymNetUp, asymAlphas.Length, alpha) + (tvc.active ? (tvHiC * IaInv[1, 1] * 57.29578f) : 0f)));
					float aAvC = Mathf.Max(CoupRollFloor, Plugin.CouplingRollLimit * netC) * (MathF.PI / 180f);
					float pLimC = Mathf.Sqrt(2f * aAvC / (kIc * s2c)) * 57.29578f;
					coupRollLim = pLimC;
					num10 = Mathf.Lerp(num10, Mathf.Min(num10, pLimC), gateC);
					num11 = Mathf.Clamp(num11, 0f - num10, num10);
				}
			}
			if (AsymRollMax > 0f && (!AsymRollHardOnly || aRollGo < aRollStop))
			{
				float asymG = Mathf.Clamp01((asymUNow - AsymRollU0) / 0.3f);
				if (asymG > 0f)
				{
					num10 = Mathf.Min(num10, Mathf.Lerp(num10, AsymRollMax, asymG));
					num11 = Mathf.Clamp(num11, 0f - num10, num10);
				}
			}
			RollGovStep(dt, num10, rollSide);
			if (rollGov < 0.999f)
			{
				num10 = Mathf.Min(num10, Mathf.Max(num10 * rollGov, RollGovMinRate));
				num11 = Mathf.Clamp(num11, 0f - num10, num10);
			}
			pSmaxDbg = num10;
			float num12 = (0f - (1f - num9)) * sy * Plugin.PedalBeta;
			betaPedalCmd = num12;
			float num13 = Mathf.Abs(beta - num12);
			float num14 = Mathf.Clamp01((Mathf.Abs(alpha) - 8f) / 10f);
			float x = Mathf.Clamp01(1f - (num13 - Plugin.SideslipGuardStart) / 10f) * (1f - Plugin.YawSatGuard * yawSat * num14);
			x = guardF.Step(x, 0.25f, dt);
			num11 *= x;
			guardDbg = x;
			pSTgtDbg = num11;
			float num15 = Mathf.Abs(pTraj.x * num);
			float max = Mathf.Lerp(Plugin.RollAccelUseLowIas, Plugin.RollAccelUse, Mathf.InverseLerp(60f, 110f, ias));
			float a = Mathf.Clamp(0.85f - num15 / Mathf.Max(30f, pFull * (aRollGo / aRoll)), 0.1f, max) * aRollGo / Mathf.Max(Mathf.Abs(num), 0.05f);
			float num16 = 0.6f * aYaw / Mathf.Max(Mathf.Abs(num2), 0.05f);
			float num17 = (latAccDbg = Mathf.Min(a, RollEntryCoord * num16));
			// 1.8.41: the roll entry's acceleration yields with the governor too (the entry is where the surfaces deflect most)
			num17 *= Mathf.Lerp(RollGovAccelFloor, 1f, rollGov);
			if (pTraj.x * (num11 - pTraj.x) < 0f)
			{
				float num18 = aRoll / Mathf.Max(30f, pFull);
				num17 = Mathf.Min((Plugin.RollBrakeUse * (BrakeUseAll ? aRollGo : aRollWing) + num18 * num15) / Mathf.Max(Mathf.Abs(num), 0.05f), BrakeYawCap * num16);
				if (num11 * pTraj.x < 0f && Mathf.Abs(num11) > 10f && ReversalFloor)
				{
					float num19 = Mathf.Min(RevFloorK * aRoll / Mathf.Max(Mathf.Abs(num), 0.05f), num16);
					num17 = (revA = (RevConst ? Mathf.Min(num19, 1.3f * num16) : Mathf.Max(num17, num19)));
					revTarget = num11;
				}
			}
			else if (revA > 0f && RevCarry)
			{
				if (num11 * revTarget <= 0f || Mathf.Abs(num11) < 10f)
				{
					revA = 0f;
				}
				else
				{
					float num20 = 1f - Mathf.Clamp01(Mathf.Abs(pTraj.x) / Mathf.Max(10f, 0.5f * Mathf.Abs(num11)));
					num17 = Mathf.Max(num17, Mathf.Lerp(num17, revA, num20));
					if (num20 <= 0f)
					{
						revA = 0f;
					}
				}
			}
			num17 = Mathf.Clamp(num17, 20f, Plugin.MaxRollAccel);
			float a2 = Mathf.Min(num17 / RollJerkTau, RollJerkServo * aRoll * rollSlew / Mathf.Max(Mathf.Abs(num), 0.05f));
			if (isAI && Plugin.AIDirectRoll)
			{
				float aDir = Mathf.Min(AIRollDirectAccel, 1.3f * num16);
				num17 = Mathf.Max(num17, aDir);
				a2 = Mathf.Max(a2, aDir / 0.05f);
			}
			latAccDbg = num17;
			latJerkDbg = Mathf.Max(a2, 100f);
			float endJ = RollEndJerk * Mathf.Lerp(1f, EndJerkHi, Mathf.InverseLerp(EndJerkLp0, EndJerkLp1, aRoll / Mathf.Max(30f, pFull)));
			lpDbg = aRoll / Mathf.Max(30f, pFull);
			pTraj.Step(num11, num17, Mathf.Max(a2, 100f), Mathf.Max(a2 * Mathf.Min(endJ, 1f), 100f), dt);
			pS = pTraj.x;
			float num21 = 0f;
			if (tas > 40f)
			{
				num21 = 9.81f * Mathf.Sin(phi * (MathF.PI / 180f)) * Mathf.Cos(theta * (MathF.PI / 180f)) / tas * 57.29578f * Mathf.Clamp01((tas - 40f) / 20f);
			}
			float num22 = ((Mode == FcsMode.GearDownRate) ? 1f : Plugin.KBeta);
			float num23 = (rSDbg = rSF.Step(Mathf.Clamp(num22 * (beta - num12), 0f - num8, num8) + num21, 0.12f, dt));
			float num24 = pS * num - num23 * num2;
			float num25 = pS * num2 + num23 * num;
			float num26 = Mathf.Max(1f, Mathf.Max(Mathf.Abs(num24) / num3, Mathf.Abs(num25) / num8));
			pCmd = num24 / num26;
			rCmd = num25 / num26;
			pDotFF = pTraj.v * num / num26;
			float num27 = aRoll / Mathf.Max(30f, pFull);
			pCmd += Plugin.RollLead * Mathf.Max(0f, num27 - 2.5f) * pDotFF;
			float t2 = Mathf.Max(Mathf.InverseLerp(200f, 300f, ias), Mathf.InverseLerp(YawFFAoA0, YawFFAoA1, Mathf.Abs(alpha)));
			rDotFF = Mathf.Lerp(Plugin.YawRateFF, 1f, t2) * rDotF.Step((rCmd - rCmdPrev) / dt, 0.1f, dt);
			pCmdPrev = pCmd;
			rCmdPrev = rCmd;
			if (Mathf.Abs(omega.z * 57.29578f) > num8 && rDotFF * omega.z > 0f)
			{
				rDotFF = 0f;
			}
		}

		public void WriteHeader(TextWriter w)
		{
			w.Write("t,mode,ias,tas,rho,alpha,beta,nz,nAlpha,tw,p,q,r,sp,sr,sy,qB,qRaw,qCmd,qT,pCmd,rCmd,aUp,aDn,betaG,alphaCmd,nCmd,unload,prot,omegaV,phi,theta,tvcCmd,tvcNow,nMaxAero,alphaAtNMax,gAvail,gInt,aInt,flapBias,flapTarget,hlPos,relief,rudderAuth,yawSat,aRoll,aYaw,pMax,pStockRef,pFull,rMax,guard,pSmax,pSTarget,latAcc,rS,stabPitchFrac,gndBrake,revFlow,asymDy,asymAr,asymAy,asymU,asymCap,asymCapEff,asymCapNeg,dMeasR,dMeasY,asymCr,asymCy,asymFFr,asymFFy,asymMr,asymMy,asymFFGate,rollPitchFF,gUnder,pitchPrio,stabRateBound,momentLead,rollTrim,aRollPos,aRollNeg,latReserve,aUpPitch,aDnPitch,aBrakeTgt,stabBranch,tvcAnchor,tvcBudget,rudSide,gsRCmd,gsNws,gsRud,gsHold,gsLineY,asymCapPitch,pitchNeed,coupRollLim,rpGate,rpBand,rollGov,rollGovMargin,iasDot,qPredK,asymCapRaw,asymCapFilt,alphaCmdRaw,nCmdRaw,rateBlendAoA,hiRate,rateW,zoneX,authCap,authCapEff,nLiftMax,hybridDMacc,floatOn,floatS,rateBlendAoANeg,nLiftMin,hiSide");
			foreach (SurfaceEffector surface in surfaces)
			{
				w.Write("," + surface.name + "_cmd," + surface.name + "_now," + surface.name + "_aoaNow," + surface.name + "_aoaCmd");
			}
			w.WriteLine();
		}

		public void WriteRow(TextWriter w, float t, float spv, float srv, float syv, float rho)
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			w.Write(string.Format(invariantCulture, "{0:F3},{1},{2:F1},{3:F1},{4:F3},{5:F2},{6:F2},{7:F2},{8:F3},{9:F2},{10:F1},{11:F1},{12:F1},{13:F2},{14:F2},{15:F2},{16:F1},{17:F1},{18:F1},{19:F1},{20:F1},{21:F1},{22:F0},{23:F0},{24:F2},{25:F1},{26:F2},{27},{28},{29:F1},{30:F1},{31:F1},{32:F2},{33:F2}", t, (int)Mode, ias, tas, rho, alpha, beta, nz, nAlpha, tw, omega.x * 57.29578f, omega.y * 57.29578f, omega.z * 57.29578f, spv, srv, syv, qB, qRaw, qCmd, qT, pCmd, rCmd, aUp, aDn, betaG, alphaCmd, nCmd, unload ? 1 : 0, prot ? 1 : 0, omegaV, phi, theta, tvc.cmd, tvc.now));
			w.Write(string.Format(invariantCulture, ",{0:F2},{1:F0},{2:F2},{3:F2},{4:F2},{5:F1},{6:F1},{7:F2},{8:F1},{9:F2},{10:F2},{11:F0},{12:F0}", nMaxAero, alphaAtNMax, gAvail, gInt, aInt, flaps.flapBias, flaps.flapTarget, flaps.hlPos, flaps.relief, rudderAuth, yawSat, aRoll, aYaw));
			w.Write(string.Format(invariantCulture, ",{0:F0},{1:F0},{2:F0},{3:F0},{4:F2},{5:F0},{6:F0},{7:F0},{8:F1},{9:F2}", pMaxDbg, pStockRef, pFull, rMaxDbg, guardDbg, pSmaxDbg, pSTgtDbg, latAccDbg, rSDbg, stabPitchFrac));
			w.Write(string.Format(invariantCulture, ",{0:F2},{1}", groundBrake, revFlow ? 1 : 0));
			w.Write(string.Format(invariantCulture, ",{0:F3},{1:F1},{2:F1},{3:F2},{4:F1},{5:F1},{6:F1},{7:F1},{8:F1}", asymDy, asymArNow, asymAyNow, asymUNow, Mathf.Min(asymCapPos, 999f), Mathf.Min(asymCapPosEff, 999f), Mathf.Max(asymCapNegEff, -999f), dMeasR, dMeasY));
			w.Write(string.Format(invariantCulture, ",{0:F0},{1:F0},{2:F1},{3:F1},{4:F1},{5:F1}", asymCrNow, asymCyNow, asymFFr, asymFFy, AccRoll(asymMNow), AccYaw(asymMNow)));
			w.Write(string.Format(invariantCulture, ",{0:F2}", asymFFGate));
			w.Write(string.Format(invariantCulture, ",{0:F1},{1:F1}", rollPitchFF * IaInv[1, 1] * 57.29578f, gUnderDbg));
			w.Write(string.Format(invariantCulture, ",{0:F1},{1},{2:F2},{3:F0},{4:F0},{5:F0},{6:F2},{7:F0},{8:F0},{9:F0},{10}", pitchPrio, stabRateBound ? 1 : 0, mLeadGate * mLeadAgree, dTrimR, aRollPos, aRollNeg, latReserve, aUpPitch, aDnPitch, aBrakeTgt, stabBranch));
			w.Write(string.Format(invariantCulture, ",{0:F2},{1:F2},{2:F1}", tvcAnchor, tvcBudgetGate, rudSideDbg));
			w.Write(string.Format(invariantCulture, ",{0:F2},{1:F2},{2:F2},{3},{4:F2},{5:F1},{6:F2},{7:F0}", gsRCmd, gsNwsDeg, gsRud, gsHold ? 1 : 0, gsLineY, Mathf.Min(asymCapPitch, 999f), pitchNeed, coupRollLim));
			w.Write(string.Format(invariantCulture, ",{0:F2},{1:F1},{2:F2},{3:F2}", rpGate, rpBand, rollGov, rollGovMargin));
			w.Write(string.Format(invariantCulture, ",{0:F2},{1:F3},{2:F1},{3:F1}", iasDotF, qPredK, Mathf.Clamp(asymCapPos, -99f, 99f), Mathf.Clamp(asymCapFilt, -99f, 99f)));
			w.Write(string.Format(invariantCulture, ",{0:F1},{1:F2},{2:F1},{3},{4:F2},{5:F2},{6:F1},{7:F1},{8:F2},{9:F1}", alphaCmdRaw, nCmdRaw, rateBlendAoA, hiRate ? 1 : 0, rateW, zoneX, Mathf.Min(authCap, 99f), Mathf.Min(authCapEff, 99f), nLiftMax, hybridDM * IaInv[1, 1] * 57.29578f));
			w.Write(string.Format(invariantCulture, ",{0},{1:F2}", floatOn ? 1 : 0, floatS));
			w.Write(string.Format(invariantCulture, ",{0:F1},{1:F2},{2}", rateBlendAoANeg, nLiftMin, hiRate ? hiSide : 0));
			foreach (SurfaceEffector surface in surfaces)
			{
				w.Write(string.Format(invariantCulture, ",{0:F2},{1:F2},{2:F1},{3:F1}", surface.cmd, surface.now, surface.localAoANow, surface.localAoACmd));
			}
			w.WriteLine();
		}
	}
}
