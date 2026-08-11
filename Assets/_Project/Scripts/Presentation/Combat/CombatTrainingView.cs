using System;
using System.Text;
using DemonLord.Domain.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DemonLord.Presentation.Combat
{
    /// <summary>
    /// Pure UGUI projection for the liaison prototype. It intentionally owns no combat math:
    /// all SP, probability, condition and cancellation values are supplied by CombatTrainingBattle.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatTrainingView : MonoBehaviour
    {
        private const int ArcSegmentCount = 8;
        private const int IntentArcCount = CombatTrainingBattle.EnemyCount * CombatTrainingBattle.UnitCount;
        private const int PlannedTargetArcCount = CombatTrainingBattle.UnitCount;
        private const int MaxTimelineNodeCount = CombatTrainingBattle.UnitCount + CombatTrainingBattle.EnemyCount;
        // The combat group is deliberately scaled as one unit. This keeps the original
        // uncut lineup, intent curves, labels and hit areas in the same proportions.
        private const float CompactBattleVisualScale = 0.78f;
        private const int ActorPresentationCount = CombatTrainingBattle.UnitCount + CombatTrainingBattle.EnemyCount;
        private const float FocusStepDistance = 14f;
        private const float FocusScale = 1.08f;
        private const float ImpactFlashDuration = 0.22f;
        private const float DefeatFadeDuration = 0.34f;

        private static readonly Color Primary = ColorFromHex("101720", 1f);
        private static readonly Color PrimaryLight = ColorFromHex("26384B", 1f);
        private static readonly Color Surface = ColorFromHex("171C22", 0.98f);
        private static readonly Color Secondary = ColorFromHex("5CAECC", 1f);
        private static readonly Color SecondaryDark = ColorFromHex("234B63", 1f);
        private static readonly Color Gold = ColorFromHex("B99A59", 1f);
        private static readonly Color MainText = ColorFromHex("EEE6D5", 1f);
        private static readonly Color MutedText = ColorFromHex("9AA6AF", 1f);
        private static readonly Color Danger = ColorFromHex("7D1827", 1f);
        private static readonly Color Error = ColorFromHex("D04A50", 1f);
        private static readonly Color SelectedGreen = ColorFromHex("77D98A", 1f);
        // Normalized actor points inside combat_training_lineup_v1. Keeping these relative
        // to the source illustration means the arrows and enemy hit targets scale with the
        // artwork rather than with an unrelated fixed battlefield coordinate.
        private static readonly Vector2[] AllyLineupPoints =
        {
            new Vector2(0.069f, 0.372f),
            new Vector2(0.207f, 0.509f),
            new Vector2(0.345f, 0.432f),
        };
        private static readonly Vector2[] AllyLineupFootPoints =
        {
            new Vector2(0.069f, 0.240f),
            new Vector2(0.207f, 0.240f),
            new Vector2(0.345f, 0.250f),
        };
        private static readonly Vector2[] EnemyLineupPoints =
        {
            new Vector2(0.650f, 0.454f),
            new Vector2(0.803f, 0.476f),
            new Vector2(0.941f, 0.476f),
        };
        private static readonly Vector2[] EnemyLineupFootPoints =
        {
            new Vector2(0.650f, 0.240f),
            new Vector2(0.803f, 0.240f),
            new Vector2(0.941f, 0.230f),
        };
        private static readonly Vector2[] EnemyLineupHitboxSizes =
        {
            new Vector2(0.160f, 0.440f),
            new Vector2(0.145f, 0.460f),
            new Vector2(0.120f, 0.450f),
        };
        // Dedicated transparent actor assets are used only during automatic playback.
        // Planning keeps the original combined illustration untouched.
        private static readonly string[] ActorSpriteResourcePaths =
        {
            "Combat/Actors/combat_actor_slime_001_v2",
            "Combat/Actors/combat_actor_skeleton_guard_v2",
            "Combat/Actors/combat_actor_goblin_archer_v2",
            "Combat/Actors/combat_actor_trainee_swordsman_v2",
            "Combat/Actors/combat_actor_trainee_shieldbearer_v2",
            "Combat/Actors/combat_actor_apprentice_mage_v2",
        };
        private static readonly Vector2[] ActorPresentationSizes =
        {
            new Vector2(123f, 123f),
            new Vector2(210f, 210f),
            new Vector2(136f, 136f),
            new Vector2(182f, 182f),
            new Vector2(194f, 194f),
            new Vector2(183f, 183f),
        };
        // Bottom-edge offsets are measured from the alpha bounds of the dedicated PNGs.
        // They put visual feet on the same ground line as the labels in the original lineup.
        private static readonly float[] ActorGroundOffsets =
        {
            39f,
            82f,
            48f,
            68f,
            71f,
            72f,
        };

        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private GameObject backgroundRoot;
        [SerializeField] private Sprite lineupSprite;
        [SerializeField] private Sprite backdropSprite;
        [SerializeField] private Image backdropImage;
        [SerializeField] private Image lineupImage;
        [SerializeField] private RectTransform compactBattleVisualRoot;
        [SerializeField] private RectTransform actorPresentationRoot;
        [SerializeField] private Image[] actorPresentationImages = Array.Empty<Image>();
        [SerializeField] private Image[] actorImpactOverlays = Array.Empty<Image>();
        [SerializeField] private Text[] actorDamagePopups = Array.Empty<Text>();
        [SerializeField] private Text roundLabel;
        [SerializeField] private Text spLabel;
        [SerializeField] private GameObject compactSpMeter;
        [SerializeField] private Text compactSpValueLabel;
        [SerializeField] private Text compactSpDeltaLabel;
        [SerializeField] private Text[] compactSpStarLabels = Array.Empty<Text>();
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text instructionLabel;
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private Text playbackLabel;
        [SerializeField] private Button[] allyRosterButtons = Array.Empty<Button>();
        [SerializeField] private Image[] allyRosterFrames = Array.Empty<Image>();
        [SerializeField] private Text[] allyRosterLabels = Array.Empty<Text>();
        [SerializeField] private Text[] allyRosterOrderLabels = Array.Empty<Text>();
        [SerializeField] private Image[] allyFieldGlows = Array.Empty<Image>();
        [SerializeField] private RectTransform[] allyFieldAnchors = Array.Empty<RectTransform>();
        [SerializeField] private Text[] allyFieldLabels = Array.Empty<Text>();
        [SerializeField] private Button[] enemyTargetButtons = Array.Empty<Button>();
        [SerializeField] private Image[] enemyFieldGlows = Array.Empty<Image>();
        [SerializeField] private RectTransform[] enemyFieldAnchors = Array.Empty<RectTransform>();
        [SerializeField] private Text[] enemyFieldLabels = Array.Empty<Text>();
        [SerializeField] private Text[] enemyIntentIconLabels = Array.Empty<Text>();
        [SerializeField] private Text[] enemyIntentLabels = Array.Empty<Text>();
        [SerializeField] private Text[] enemyConditionLabels = Array.Empty<Text>();
        [SerializeField] private GameObject skillPanel;
        [SerializeField] private Text selectedUnitLabel;
        [SerializeField] private Button[] skillButtons = Array.Empty<Button>();
        [SerializeField] private Text[] skillButtonLabels = Array.Empty<Text>();
        [SerializeField] private Image[] timelineSlotBackgrounds = Array.Empty<Image>();
        [SerializeField] private Text[] timelineSlotLabels = Array.Empty<Text>();
        [SerializeField] private Text[] timelineSpeedChipLabels = Array.Empty<Text>();
        [SerializeField] private Button executeButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitleLabel;
        [SerializeField] private Text resultBodyLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button resultExitButton;

        private readonly CombatTrainingSkillId[] visibleSkillIds = new CombatTrainingSkillId[2];
        // These are scene references, not transient render state. A curve is composed from
        // small UI segments so intent lines can arc over the combat illustration.
        [SerializeField] private Image[] enemyIntentArrowSegments = new Image[IntentArcCount * ArcSegmentCount];
        [SerializeField] private Image[] enemyIntentArrowHeads = new Image[IntentArcCount];
        // A committed skill keeps its own cyan target curve. The single preview curve remains
        // brighter and follows the unit the player is currently editing.
        [SerializeField] private Image[] plannedTargetArcSegments = new Image[PlannedTargetArcCount * ArcSegmentCount];
        [SerializeField] private Image[] plannedTargetArcHeads = new Image[PlannedTargetArcCount];
        [SerializeField] private Image[] targetPreviewArcSegments = new Image[ArcSegmentCount];
        [SerializeField] private Image targetPreviewHead;
        [SerializeField] private RectTransform compactArrowLayer;
        // Retained only so an older serialized overlay can compile until the World builder
        // regenerates it. The compact layout never renders these straight-line references.
        [SerializeField] private Image[] enemyIntentArrowLines = new Image[IntentArcCount];
        [SerializeField] private Image targetPreviewLine;
        private bool listenersBound;
        private bool unavailableNotificationRaised;
        private bool compactLayoutBuilt;
        private int focusedTimelineIndex = -1;
        private CombatTrainingBattle renderedBattle;
        private readonly Vector2[] actorPresentationBasePositions = new Vector2[ActorPresentationCount];
        private readonly Vector2[] actorPresentationBaseSizes = new Vector2[ActorPresentationCount];
        private readonly bool[] actorPresentationAlive = new bool[ActorPresentationCount];
        private readonly bool[] actorDefeatPending = new bool[ActorPresentationCount];
        private readonly int[] impactActorIndices = new int[CombatTrainingBattle.UnitCount];
        private readonly int[] impactDamageValues = new int[CombatTrainingBattle.UnitCount];
        private int focusedActorPresentationIndex = -1;
        private int impactActorCount;
        private float focusStartedAt = -1f;
        private float impactStartedAt = -1f;

        public event Action<int> AllyRosterRequested;
        public event Action<int> EnemyTargetRequested;
        public event Action<CombatTrainingSkillId> SkillRequested;
        public event Action ExecuteRequested;
        public event Action RetryRequested;
        public event Action ExitRequested;
        public event Action BecameUnavailable;

        public bool IsVisible => overlayGroup != null && overlayGroup.alpha > 0.99f && overlayGroup.blocksRaycasts;
        public CanvasGroup OverlayGroup => overlayGroup;

        private void Awake()
        {
            // The scene can enter play without a second Configure call. Restore the
            // compact-layout marker before the coordinator's liaison callback renders.
            compactLayoutBuilt = HasCompactLayoutReferences();
            EnsureCompactSpMeter(null);
            EnsureActorPresentation();
            BindButtonListeners();
            SetVisible(false);
        }

        private void Update()
        {
            RefreshActorPresentationAnimation();
        }

        private void OnEnable()
        {
            unavailableNotificationRaised = false;
        }

        private void OnDisable()
        {
            NotifyBecameUnavailable();
        }

        private void OnDestroy()
        {
            NotifyBecameUnavailable();
        }

        public void Configure(Font font, EventSystem configuredEventSystem)
        {
            Configure(font, configuredEventSystem, lineupSprite, backdropSprite);
        }

        public void Configure(Font font, EventSystem configuredEventSystem, Sprite configuredLineupSprite)
        {
            Configure(font, configuredEventSystem, configuredLineupSprite, backdropSprite);
        }

        public void Configure(
            Font font,
            EventSystem configuredEventSystem,
            Sprite configuredLineupSprite,
            Sprite configuredBackdropSprite)
        {
            eventSystem = configuredEventSystem;
            if (configuredLineupSprite != null)
            {
                lineupSprite = configuredLineupSprite;
            }

            if (configuredBackdropSprite != null)
            {
                backdropSprite = configuredBackdropSprite;
            }

            if (lineupSprite == null)
            {
                lineupSprite = Resources.Load<Sprite>("Combat/combat_training_lineup_v1");
            }

            if (overlayGroup == null)
            {
                overlayGroup = GetComponent<CanvasGroup>();
                if (overlayGroup == null)
                {
                    overlayGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (backgroundRoot == null)
            {
                Build(font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            }
            else if (lineupImage != null && lineupSprite != null)
            {
                lineupImage.sprite = lineupSprite;
                lineupImage.color = Color.white;
            }

            if (backdropImage != null && backdropSprite != null)
            {
                backdropImage.sprite = backdropSprite;
                backdropImage.color = Color.white;
            }

            EnsureActorPresentation();
            // This flag is runtime-only. Reconstruct it from the serialized curve references
            // whenever a regenerated scene is loaded, otherwise RenderArrows would fall back
            // to the retired straight-line layout after a domain reload.
            compactLayoutBuilt = compactLayoutBuilt || HasCompactLayoutReferences();
            EnsureCompactSpMeter(font);
            BindButtonListeners();
            SetVisible(false);
        }

        private bool HasCompactLayoutReferences()
        {
            return targetPreviewHead != null
                && compactArrowLayer != null
                && targetPreviewArcSegments != null
                && targetPreviewArcSegments.Length == ArcSegmentCount
                && plannedTargetArcSegments != null
                && plannedTargetArcSegments.Length == PlannedTargetArcCount * ArcSegmentCount
                && plannedTargetArcHeads != null
                && plannedTargetArcHeads.Length == PlannedTargetArcCount
                && enemyIntentArrowSegments != null
                && enemyIntentArrowSegments.Length == IntentArcCount * ArcSegmentCount
                && enemyIntentArrowHeads != null
                && enemyIntentArrowHeads.Length == IntentArcCount;
        }

        public bool TryValidateConfiguration(out string error)
        {
            error = null;
            if (overlayGroup == null || backgroundRoot == null || lineupImage == null
                || roundLabel == null || spLabel == null || phaseLabel == null || instructionLabel == null
                || feedbackLabel == null || playbackLabel == null || skillPanel == null || selectedUnitLabel == null
                || executeButton == null || exitButton == null || resultPanel == null || retryButton == null || resultExitButton == null
                || compactArrowLayer == null)
            {
                error = "combat_view_reference_missing";
                return false;
            }

            if (allyRosterButtons.Length != CombatTrainingBattle.UnitCount
                || allyRosterFrames.Length != CombatTrainingBattle.UnitCount
                || allyRosterLabels.Length != CombatTrainingBattle.UnitCount
                || allyRosterOrderLabels.Length != CombatTrainingBattle.UnitCount
                || allyFieldGlows.Length != CombatTrainingBattle.UnitCount
                || allyFieldAnchors.Length != CombatTrainingBattle.UnitCount
                || allyFieldLabels.Length != CombatTrainingBattle.UnitCount
                || enemyTargetButtons.Length != CombatTrainingBattle.EnemyCount
                || enemyFieldGlows.Length != CombatTrainingBattle.EnemyCount
                || enemyFieldAnchors.Length != CombatTrainingBattle.EnemyCount
                || enemyFieldLabels.Length != CombatTrainingBattle.EnemyCount
                || enemyIntentIconLabels.Length != CombatTrainingBattle.EnemyCount
                || enemyIntentLabels.Length != CombatTrainingBattle.EnemyCount
                || enemyConditionLabels.Length != CombatTrainingBattle.EnemyCount
                || skillButtons.Length != 2
                || skillButtonLabels.Length != 2
                || timelineSlotBackgrounds.Length != MaxTimelineNodeCount
                || timelineSlotLabels.Length != MaxTimelineNodeCount
                || timelineSpeedChipLabels.Length != MaxTimelineNodeCount)
            {
                error = "combat_view_array_size_invalid";
                return false;
            }

            if (targetPreviewArcSegments.Length != ArcSegmentCount || targetPreviewHead == null)
            {
                error = "combat_target_arrow_missing";
                return false;
            }

            if (plannedTargetArcSegments.Length != PlannedTargetArcCount * ArcSegmentCount
                || plannedTargetArcHeads.Length != PlannedTargetArcCount)
            {
                error = "combat_planned_target_arrow_count_invalid";
                return false;
            }

            if (enemyIntentArrowSegments.Length != IntentArcCount * ArcSegmentCount
                || enemyIntentArrowHeads.Length != IntentArcCount)
            {
                error = "combat_intent_arrow_count_invalid";
                return false;
            }

            for (int index = 0; index < targetPreviewArcSegments.Length; index++)
            {
                if (targetPreviewArcSegments[index] == null)
                {
                    error = "combat_target_arrow_missing";
                    return false;
                }
            }

            for (int index = 0; index < plannedTargetArcSegments.Length; index++)
            {
                if (plannedTargetArcSegments[index] == null)
                {
                    error = "combat_planned_target_arrow_missing";
                    return false;
                }
            }

            for (int index = 0; index < plannedTargetArcHeads.Length; index++)
            {
                if (plannedTargetArcHeads[index] == null)
                {
                    error = "combat_planned_target_arrow_missing";
                    return false;
                }
            }

            for (int index = 0; index < enemyIntentArrowSegments.Length; index++)
            {
                if (enemyIntentArrowSegments[index] == null)
                {
                    error = "combat_intent_arrow_missing";
                    return false;
                }
            }

            for (int index = 0; index < enemyIntentArrowHeads.Length; index++)
            {
                if (enemyIntentArrowHeads[index] == null)
                {
                    error = "combat_intent_arrow_missing";
                    return false;
                }
            }

            Transform[] descendants = GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < descendants.Length; index++)
            {
                string childName = descendants[index].name;
                if (string.Equals(childName, "Timing", StringComparison.Ordinal)
                    || string.Equals(childName, "TimingButton", StringComparison.Ordinal)
                    || string.Equals(childName, "DefensePlanning", StringComparison.Ordinal)
                    || string.Equals(childName, "ApprovalLine", StringComparison.Ordinal))
                {
                    error = "legacy_combat_ui_present";
                    return false;
                }
            }

            return true;
        }

        public void Show()
        {
            transform.SetAsLastSibling();
            SetVisible(true);
        }

        public void Hide()
        {
            ClearPlaybackFocus();
            SetVisible(false);
        }

        public void Render(
            CombatTrainingBattle battle,
            CombatTrainingUnitId selectedUnit,
            CombatTrainingEnemyId selectedTarget,
            string feedback = null)
        {
            if (battle == null || backgroundRoot == null)
            {
                return;
            }

            renderedBattle = battle;
            roundLabel.text = "ROUND " + battle.Round;
            spLabel.text = "공유 SP  " + battle.SharedSp + " / " + CombatTrainingBattle.MaximumSp;
            phaseLabel.text = PhaseLabel(battle.Phase);
            instructionLabel.text = Instruction(battle.Phase);
            if (feedback != null)
            {
                feedbackLabel.text = Shorten(feedback, 68);
            }

            bool planning = battle.Phase == CombatTrainingPhase.Planning;
            bool finished = battle.IsFinished;
            skillPanel.SetActive(planning);
            executeButton.gameObject.SetActive(planning);
            resultPanel.SetActive(finished);

            RenderRoster(battle, selectedUnit, planning);
            RenderCompactSpMeter(battle);
            RenderBattlefield(battle, selectedUnit, selectedTarget, planning);
            RenderSkillSet(battle, selectedUnit, selectedTarget, planning);
            RenderTimeline(battle, planning);
            RenderArrows(battle, selectedUnit, selectedTarget, planning);

            if (finished)
            {
                bool victory = battle.Phase == CombatTrainingPhase.Victory;
                resultTitleLabel.text = victory ? "모의전 승리" : "모의전 패배";
                resultTitleLabel.color = victory ? Gold : Error;
                resultBodyLabel.text = victory
                    ? "적의 공개 행동을 행동선으로 끊어냈습니다.\n공유 SP와 선행 상태가 만든 결과를 확인하세요."
                    : "적 행동이 아군 슬롯 사이로 들어옵니다.\n공격 순서와 SP 흐름을 다시 설계하세요.";
            }

            ApplyPlaybackFocus();
            SelectDefaultIfNeeded(battle, planning);
        }

        public void ShowPlaybackFocus(CombatTimelineEntry entry, string message)
        {
            BeginActionPresentation(entry, message);
        }

        public void PlayActionImpact(CombatTimelineEntry entry, CombatTimelineResolution resolution, string message)
        {
            TriggerImpactPresentation(entry, resolution, message);
        }

        public void ClearPlaybackFocus()
        {
            focusedTimelineIndex = -1;
            focusedActorPresentationIndex = -1;
            focusStartedAt = -1f;
            impactStartedAt = -1f;
            impactActorCount = 0;
            for (int index = 0; index < actorDefeatPending.Length; index++)
            {
                actorDefeatPending[index] = false;
                if (index < actorDamagePopups.Length && actorDamagePopups[index] != null)
                {
                    actorDamagePopups[index].gameObject.SetActive(false);
                }
            }
            if (playbackLabel != null)
            {
                playbackLabel.text = string.Empty;
            }

            ApplyPlaybackFocus();
        }

        private void NotifyBecameUnavailable()
        {
            if (unavailableNotificationRaised)
            {
                return;
            }

            unavailableNotificationRaised = true;
            SetVisible(false);
            BecameUnavailable?.Invoke();
        }

        private void RenderRoster(CombatTrainingBattle battle, CombatTrainingUnitId selectedUnit, bool planning)
        {
            for (int index = 0; index < CombatTrainingBattle.UnitCount; index++)
            {
                CombatTrainingUnitId unitId = (CombatTrainingUnitId)index;
                CombatTrainingUnitDefinition unit = CombatTrainingBattle.GetUnitDefinition(unitId);
                int hp = battle.GetAllyHp(unitId);
                bool alive = hp > 0;
                int slot = FindAllyActionSlot(battle, unitId);
                bool selected = alive && unitId == selectedUnit;
                allyRosterFrames[index].color = !alive
                    ? new Color(0.07f, 0.08f, 0.10f, 0.94f)
                    : selected
                        ? new Color(SelectedGreen.r * 0.38f, SelectedGreen.g * 0.48f, SelectedGreen.b * 0.42f, 0.98f)
                        : new Color(SecondaryDark.r, SecondaryDark.g, SecondaryDark.b, 0.94f);
                allyRosterButtons[index].interactable = planning && alive;
                int timelineIndex = FindTimelineIndexForAllySlot(battle, slot);
                if (compactLayoutBuilt)
                {
                    allyRosterLabels[index].text = (selected ? "▶ " : string.Empty) + Shorten(unit.DisplayName, 12);
                    allyRosterOrderLabels[index].text = "HP " + hp + "/" + unit.MaximumHp
                        + (timelineIndex >= 0 ? " · " + (timelineIndex + 1) : string.Empty);
                }
                else
                {
                    allyRosterLabels[index].text = (selected ? "선택  " : string.Empty) + unit.DisplayName
                        + "\n<size=15>HP " + hp + " / " + unit.MaximumHp + "</size>";
                    allyRosterOrderLabels[index].text = timelineIndex >= 0
                        ? "행동선 " + (timelineIndex + 1)
                        : "미배정";
                }

                allyRosterOrderLabels[index].color = timelineIndex >= 0 ? Gold : MutedText;
            }
        }

        private void RenderBattlefield(
            CombatTrainingBattle battle,
            CombatTrainingUnitId selectedUnit,
            CombatTrainingEnemyId selectedTarget,
            bool planning)
        {
            if (compactLayoutBuilt)
            {
                RenderCompactBattlefield(battle, selectedUnit, selectedTarget, planning);
                return;
            }

            for (int index = 0; index < CombatTrainingBattle.UnitCount; index++)
            {
                CombatTrainingUnitId unitId = (CombatTrainingUnitId)index;
                CombatTrainingUnitDefinition unit = CombatTrainingBattle.GetUnitDefinition(unitId);
                int hp = battle.GetAllyHp(unitId);
                bool alive = hp > 0;
                allyFieldGlows[index].color = !alive
                    ? new Color(0f, 0f, 0f, 0.52f)
                    : unitId == selectedUnit
                        ? new Color(SelectedGreen.r, SelectedGreen.g, SelectedGreen.b, 0.36f)
                        : new Color(Secondary.r, Secondary.g, Secondary.b, 0.08f);
                allyFieldLabels[index].text = unit.DisplayName + "\n<size=15>HP " + hp + " / " + unit.MaximumHp + "</size>";
                allyFieldLabels[index].color = alive ? MainText : MutedText;
            }

            for (int index = 0; index < CombatTrainingBattle.EnemyCount; index++)
            {
                CombatTrainingEnemyId enemyId = (CombatTrainingEnemyId)index;
                CombatTrainingEnemyDefinition enemy = CombatTrainingBattle.GetEnemyDefinition(enemyId);
                int hp = battle.GetEnemyHp(enemyId);
                bool alive = hp > 0;
                bool selected = alive && enemyId == selectedTarget;
                enemyFieldGlows[index].color = !alive
                    ? new Color(0f, 0f, 0f, 0.58f)
                    : selected
                        ? new Color(Gold.r, Gold.g, Gold.b, 0.34f)
                        : new Color(Danger.r, Danger.g, Danger.b, 0.10f);
                enemyTargetButtons[index].interactable = planning && alive;
                enemyFieldLabels[index].text = enemy.DisplayName + "\n<size=15>HP " + hp + " / " + enemy.MaximumHp + "</size>";
                enemyFieldLabels[index].color = alive ? MainText : MutedText;

                CombatTrainingEnemyIntent intent = battle.GetEnemyIntent(enemyId);
                battle.GetProjectedEnemyDamageRange(enemyId, out int minimumDamage, out int maximumDamage, out bool mayBeCancelled);
                int timelineIndex = FindTimelineIndexForEnemy(battle, enemyId);
                bool cancelledNow = timelineIndex >= 0 && battle.GetTimelineEntry(timelineIndex).IsCancelled;
                enemyIntentIconLabels[index].text = intent.TargetKind == CombatIntentTargetKind.All ? "✦" : "!";
                enemyIntentIconLabels[index].color = alive && !cancelledNow ? Error : MutedText;
                string target = intent.TargetKind == CombatIntentTargetKind.All
                    ? "아군 전체"
                    : CombatTrainingBattle.GetUnitDefinition(intent.TargetUnitId).DisplayName;
                string damage = FormatDamageRange(minimumDamage, maximumDamage);
                enemyIntentLabels[index].text = alive
                    ? "다음 행동 · " + intent.DisplayName + "\n→ " + target + "  피해 " + damage
                        + (mayBeCancelled ? "  (취소 가능)" : string.Empty)
                    : "행동 취소 · 처치됨";
                enemyIntentLabels[index].color = alive ? Error : MutedText;
                enemyConditionLabels[index].text = ConditionSummary(battle.GetEnemyConditions(enemyId));
            }
        }

        private void RenderSkillSet(
            CombatTrainingBattle battle,
            CombatTrainingUnitId selectedUnit,
            CombatTrainingEnemyId selectedTarget,
            bool planning)
        {
            if (compactLayoutBuilt)
            {
                RenderCompactSkillSet(battle, selectedUnit, selectedTarget, planning);
                return;
            }

            CombatTrainingUnitDefinition unit = CombatTrainingBattle.GetUnitDefinition(selectedUnit);
            selectedUnitLabel.text = "선택 부대\n" + unit.DisplayName + "\n<size=16>대상: "
                + CombatTrainingBattle.GetEnemyDefinition(selectedTarget).DisplayName + "</size>";
            selectedUnitLabel.color = battle.IsUnitAlive(selectedUnit) ? SelectedGreen : MutedText;

            int firstSkillIndex = (int)selectedUnit * 2;
            for (int option = 0; option < 2; option++)
            {
                CombatTrainingSkillId skillId = (CombatTrainingSkillId)(firstSkillIndex + option);
                visibleSkillIds[option] = skillId;
                CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(skillId);
                bool assigned = TryGetAssignedSkill(battle, selectedUnit, out CombatTrainingSkillId assignedSkill)
                    && assignedSkill == skillId;
                string sp = skill.SpCost > 0 ? "SP -" + skill.SpCost : "SP +" + skill.SpGain;
                skillButtonLabels[option].text = (assigned ? "[배정] " : string.Empty) + skill.DisplayName + "  " + sp
                    + "\n확정: " + GuaranteedEffectSummary(skill)
                    + "\n보너스: +" + skill.BonusPower + " 피해 · 기본 " + skill.BaseBonusChance + "%"
                    + "\n<size=13>" + BonusConditionHint(skill.Id) + "</size>";
                skillButtons[option].interactable = planning
                    && battle.IsUnitAlive(selectedUnit)
                    && battle.IsEnemyAlive(selectedTarget);
                Image image = skillButtons[option].GetComponent<Image>();
                image.color = assigned ? new Color(Gold.r * 0.34f, Gold.g * 0.31f, Gold.b * 0.20f, 1f) : SecondaryDark;
            }
        }

        private void RenderTimeline(CombatTrainingBattle battle, bool planning)
        {
            if (compactLayoutBuilt)
            {
                RenderCompactTimeline(battle, planning);
                return;
            }

            for (int timelineIndex = 0; timelineIndex < battle.TimelineEntryCount; timelineIndex++)
            {
                CombatTimelineEntry entry = battle.GetTimelineEntry(timelineIndex);
                bool ally = entry.Side == CombatTimelineSide.Ally;
                Color normal = ally ? SecondaryDark : new Color(Danger.r, Danger.g, Danger.b, 0.74f);
                Color current = timelineIndex == focusedTimelineIndex ? Gold : normal;
                if (entry.IsCancelled && (entry.WasResolved || battle.Phase == CombatTrainingPhase.Resolving))
                {
                    current = new Color(MutedText.r, MutedText.g, MutedText.b, 0.42f);
                }

                timelineSlotBackgrounds[timelineIndex].color = current;
                if (ally)
                {
                    int slot = entry.AllySlotIndex;
                    bool occupied = entry.HasAllyAction;
                    if (!occupied)
                    {
                        timelineSlotLabels[timelineIndex].text = (timelineIndex + 1) + "\n아군 행동\n미배정";
                        timelineSlotLabels[timelineIndex].color = MutedText;
                        continue;
                    }

                    CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(entry.AllyAction.SkillId);
                    CombatTrainingEnemyDefinition target = CombatTrainingBattle.GetEnemyDefinition(entry.AllyAction.TargetEnemyId);
                    CombatActionPreview preview = battle.GetActionPreview(slot);
                    string chance = preview.HasChanceRange
                        ? preview.MinimumBonusChance + "~" + preview.MaximumBonusChance + "%"
                        : preview.MaximumBonusChance + "%";
                    string cancellation = preview.MayBeCancelled ? "\n<color=#9AA6AF>취소 분기 있음</color>" : string.Empty;
                    string cancelled = entry.IsCancelled && entry.WasResolved
                        ? "\n<color=#9AA6AF>취소 · " + SkipReasonLabel(entry.SkipReason) + "</color>"
                        : string.Empty;
                    timelineSlotLabels[timelineIndex].text = (timelineIndex + 1) + " · 아군\n" + skill.DisplayName
                        + " → " + target.DisplayName
                        + "\nSP " + preview.ProjectedSpBefore + "→" + preview.ProjectedSpAfter + " · " + chance
                        + cancellation + cancelled;
                    timelineSlotLabels[timelineIndex].color = preview.CanPay ? MainText : Error;
                }
                else
                {
                    CombatTrainingEnemyDefinition enemy = CombatTrainingBattle.GetEnemyDefinition(entry.EnemyId);
                    CombatTrainingEnemyIntent intent = entry.EnemyIntent;
                    string target = intent.TargetKind == CombatIntentTargetKind.All
                        ? "아군 전체"
                        : CombatTrainingBattle.GetUnitDefinition(intent.TargetUnitId).DisplayName;
                    string cancelled = entry.IsCancelled && entry.WasResolved
                        ? "\n<color=#9AA6AF>취소 · " + SkipReasonLabel(entry.SkipReason) + "</color>"
                        : string.Empty;
                    timelineSlotLabels[timelineIndex].text = (timelineIndex + 1) + " · 적\n" + enemy.DisplayName
                        + "\n" + intent.DisplayName + " → " + target + cancelled;
                    timelineSlotLabels[timelineIndex].color = entry.IsCancelled && entry.WasResolved ? MutedText : MainText;
                }

            }

            executeButton.interactable = battle.CanExecutePlan;
        }

        private void RenderArrows(
            CombatTrainingBattle battle,
            CombatTrainingUnitId selectedUnit,
            CombatTrainingEnemyId selectedTarget,
            bool planning)
        {
            if (compactLayoutBuilt)
            {
                RenderCompactArrows(battle, selectedUnit, selectedTarget, planning);
                return;
            }

            for (int enemyIndex = 0; enemyIndex < CombatTrainingBattle.EnemyCount; enemyIndex++)
            {
                CombatTrainingEnemyId enemyId = (CombatTrainingEnemyId)enemyIndex;
                CombatTrainingEnemyIntent intent = battle.GetEnemyIntent(enemyId);
                int timelineIndex = FindTimelineIndexForEnemy(battle, enemyId);
                bool enemyCanAct = timelineIndex >= 0
                    && battle.IsEnemyAlive(enemyId)
                    && !battle.GetTimelineEntry(timelineIndex).IsCancelled;
                for (int allyIndex = 0; allyIndex < CombatTrainingBattle.UnitCount; allyIndex++)
                {
                    bool relevantTarget = intent.TargetKind == CombatIntentTargetKind.All
                        || (int)intent.TargetUnitId == allyIndex;
                    bool active = enemyCanAct && relevantTarget && battle.IsUnitAlive((CombatTrainingUnitId)allyIndex);
                    int arrowIndex = enemyIndex * CombatTrainingBattle.UnitCount + allyIndex;
                    enemyIntentArrowLines[arrowIndex].gameObject.SetActive(active);
                    enemyIntentArrowHeads[arrowIndex].gameObject.SetActive(active);
                    if (active)
                    {
                        SetLineBetween(
                            enemyIntentArrowLines[arrowIndex].rectTransform,
                            enemyIntentArrowHeads[arrowIndex].rectTransform,
                            enemyFieldAnchors[enemyIndex].anchoredPosition,
                            allyFieldAnchors[allyIndex].anchoredPosition,
                            Error);
                    }
                }
            }

            bool targetActive = planning
                && battle.IsUnitAlive(selectedUnit)
                && battle.IsEnemyAlive(selectedTarget);
            targetPreviewLine.gameObject.SetActive(targetActive);
            targetPreviewHead.gameObject.SetActive(targetActive);
            if (targetActive)
            {
                SetLineBetween(
                    targetPreviewLine.rectTransform,
                    targetPreviewHead.rectTransform,
                    allyFieldAnchors[(int)selectedUnit].anchoredPosition,
                    enemyFieldAnchors[(int)selectedTarget].anchoredPosition,
                    Secondary);
            }
        }

        private void ApplyPlaybackFocus()
        {
            for (int index = 0; index < timelineSlotBackgrounds.Length; index++)
            {
                if (timelineSlotBackgrounds[index] != null && index == focusedTimelineIndex)
                {
                    timelineSlotBackgrounds[index].rectTransform.localScale = Vector3.one * 1.045f;
                }
                else if (timelineSlotBackgrounds[index] != null)
                {
                    timelineSlotBackgrounds[index].rectTransform.localScale = Vector3.one;
                }
            }

            for (int unit = 0; unit < allyFieldGlows.Length; unit++)
            {
                int timelineIndex = FindTimelineIndexForUnit(unit);
                allyFieldGlows[unit].rectTransform.localScale = timelineIndex == focusedTimelineIndex
                    ? Vector3.one * 1.12f
                    : Vector3.one;
            }

            for (int enemy = 0; enemy < enemyFieldGlows.Length; enemy++)
            {
                int timelineIndex = FindTimelineIndexForEnemy(renderedBattle, (CombatTrainingEnemyId)enemy);
                enemyFieldGlows[enemy].rectTransform.localScale = timelineIndex == focusedTimelineIndex
                    ? Vector3.one * 1.12f
                    : Vector3.one;
            }

            RefreshActorPresentationAnimation();
        }

        private int FindTimelineIndexForUnit(int unitIndex)
        {
            if (renderedBattle == null)
            {
                return -1;
            }

            for (int timelineIndex = 0; timelineIndex < renderedBattle.TimelineEntryCount; timelineIndex++)
            {
                CombatTimelineEntry entry = TryGetTimelineEntryForFocus(timelineIndex);
                if (entry.Side == CombatTimelineSide.Ally
                    && entry.HasAllyAction
                    && CombatTrainingBattle.GetSkillDefinition(entry.AllyAction.SkillId).ActorId == (CombatTrainingUnitId)unitIndex)
                {
                    return timelineIndex;
                }
            }

            return -1;
        }

        private static int FindTimelineIndexForAllySlot(CombatTrainingBattle battle, int allySlotIndex)
        {
            if (battle == null || allySlotIndex < 0)
            {
                return -1;
            }

            for (int timelineIndex = 0; timelineIndex < battle.TimelineEntryCount; timelineIndex++)
            {
                CombatTimelineEntry entry = battle.GetTimelineEntry(timelineIndex);
                if (entry.Side == CombatTimelineSide.Ally
                    && entry.HasAllyAction
                    && entry.AllySlotIndex == allySlotIndex)
                {
                    return timelineIndex;
                }
            }

            return -1;
        }

        private static int FindTimelineIndexForEnemy(CombatTrainingBattle battle, CombatTrainingEnemyId enemyId)
        {
            if (battle == null)
            {
                return -1;
            }

            for (int timelineIndex = 0; timelineIndex < battle.TimelineEntryCount; timelineIndex++)
            {
                CombatTimelineEntry entry = battle.GetTimelineEntry(timelineIndex);
                if (entry.Side == CombatTimelineSide.Enemy && entry.EnemyId == enemyId)
                {
                    return timelineIndex;
                }
            }

            return -1;
        }

        private CombatTimelineEntry TryGetTimelineEntryForFocus(int timelineIndex)
        {
            return renderedBattle != null
                ? renderedBattle.GetTimelineEntry(timelineIndex)
                : default;
        }

        private void SelectDefaultIfNeeded(CombatTrainingBattle battle, bool planning)
        {
            if (!IsVisible || eventSystem == null)
            {
                return;
            }

            GameObject current = eventSystem.currentSelectedGameObject;
            if (current != null && current.transform.IsChildOf(transform) && current.activeInHierarchy)
            {
                return;
            }

            Button target = battle.IsFinished ? retryButton : null;
            if (target == null && planning)
            {
                for (int index = 0; index < allyRosterButtons.Length; index++)
                {
                    if (allyRosterButtons[index].interactable)
                    {
                        target = allyRosterButtons[index];
                        break;
                    }
                }
            }

            if (target != null)
            {
                eventSystem.SetSelectedGameObject(target.gameObject);
            }
        }

        private void BindButtonListeners()
        {
            if (listenersBound
                || allyRosterButtons == null
                || allyRosterButtons.Length != CombatTrainingBattle.UnitCount
                || enemyTargetButtons == null
                || enemyTargetButtons.Length != CombatTrainingBattle.EnemyCount
                || skillButtons == null
                || skillButtons.Length != 2)
            {
                return;
            }

            for (int index = 0; index < allyRosterButtons.Length; index++)
            {
                int captured = index;
                allyRosterButtons[index].onClick.AddListener(() => AllyRosterRequested?.Invoke(captured));
            }

            for (int index = 0; index < enemyTargetButtons.Length; index++)
            {
                int captured = index;
                enemyTargetButtons[index].onClick.AddListener(() => EnemyTargetRequested?.Invoke(captured));
            }

            for (int index = 0; index < skillButtons.Length; index++)
            {
                int captured = index;
                skillButtons[index].onClick.AddListener(() => SkillRequested?.Invoke(visibleSkillIds[captured]));
            }

            executeButton.onClick.AddListener(() => ExecuteRequested?.Invoke());
            retryButton.onClick.AddListener(() => RetryRequested?.Invoke());
            exitButton.onClick.AddListener(() => ExitRequested?.Invoke());
            resultExitButton.onClick.AddListener(() => ExitRequested?.Invoke());
            listenersBound = true;
        }

        private void Build(Font font)
        {
            // Configure always supplies a usable fallback font. Keeping the legacy branch
            // below lets old serialized overlays remain readable until the scene is rebuilt.
            if (font != null)
            {
                BuildCompact(font);
                return;
            }

            backgroundRoot = CreateImage("Background", transform, Primary, true).gameObject;
            SetStretch(backgroundRoot.GetComponent<RectTransform>());

            Image header = CreateImage("Header", backgroundRoot.transform, Surface, false);
            SetRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(1920f, 96f));
            Text title = CreateText("Title", header.transform, font, 28, TextAnchor.MiddleLeft, new Vector2(55f, 48f), new Vector2(650f, 60f));
            title.text = "전투 대응 모의전 · 통합 행동선";
            title.color = Gold;
            roundLabel = CreateText("Round", header.transform, font, 24, TextAnchor.MiddleCenter, new Vector2(920f, 48f), new Vector2(250f, 56f));
            spLabel = CreateText("SharedSp", header.transform, font, 23, TextAnchor.MiddleCenter, new Vector2(1240f, 48f), new Vector2(360f, 56f));
            spLabel.color = Secondary;
            exitButton = CreateButton("Exit", header.transform, font, "훈련 종료", new Vector2(1745f, 48f), new Vector2(250f, 54f), out _);

            phaseLabel = CreateText("Phase", backgroundRoot.transform, font, 22, TextAnchor.MiddleCenter, new Vector2(960f, 958f), new Vector2(700f, 34f));
            phaseLabel.color = Gold;

            BuildRoster(font);
            BuildBattleStage(font);
            BuildSkillPanel(font);
            BuildTimeline(font);
            BuildResultPanel(font);
        }

        private void BuildCompact(Font font)
        {
            backgroundRoot = CreateImage("Background", transform, Primary, true).gameObject;
            SetStretch(backgroundRoot.GetComponent<RectTransform>());

            // The battle illustration owns the whole screen. Every other control is an
            // overlay, so the player reads the battlefield before the interface.
            backdropImage = CreateImage("CombatBackdrop", backgroundRoot.transform, Color.white, false);
            SetStretch(backdropImage.rectTransform);
            backdropImage.sprite = backdropSprite;
            backdropImage.preserveAspect = false;
            if (backdropSprite == null)
            {
                backdropImage.color = Primary;
            }

            Image backdropShade = CreateImage("CombatBackdropShade", backgroundRoot.transform, new Color(0.01f, 0.03f, 0.06f, 0.32f), false);
            SetStretch(backdropShade.rectTransform);

            Image header = CreateImage("Header", backgroundRoot.transform, new Color(Surface.r, Surface.g, Surface.b, 0.68f), false);
            SetTopStretch(header.rectTransform, 76f);
            Text title = CreateText("Title", header.transform, font, 22, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(520f, 48f));
            SetTopLeft(title.rectTransform, new Vector2(24f, -14f), new Vector2(520f, 48f));
            title.text = "전투 대응 모의전  ·  행동선 편성";
            title.color = Gold;
            roundLabel = CreateText("Round", header.transform, font, 19, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(160f, 42f));
            SetTopCenter(roundLabel.rectTransform, new Vector2(-110f, -17f), new Vector2(160f, 42f));
            spLabel = CreateText("SharedSp", header.transform, font, 19, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(230f, 42f));
            SetTopCenter(spLabel.rectTransform, new Vector2(120f, -17f), new Vector2(230f, 42f));
            spLabel.color = Secondary;
            phaseLabel = CreateText("Phase", header.transform, font, 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(210f, 38f));
            SetTopCenter(phaseLabel.rectTransform, new Vector2(375f, -19f), new Vector2(210f, 38f));
            phaseLabel.color = Gold;
            exitButton = CreateButton("Exit", header.transform, font, "훈련 종료", Vector2.zero, new Vector2(190f, 42f), out _);
            SetTopRight(exitButton.GetComponent<RectTransform>(), new Vector2(-24f, -17f), new Vector2(190f, 42f));

            // The feedback line stays compact above the illustration. The previous large
            // multi-line instruction block is deliberately removed from the composition.
            instructionLabel = CreateText("Instruction", backgroundRoot.transform, font, 1, TextAnchor.MiddleCenter, new Vector2(-100f, -100f), Vector2.one);
            instructionLabel.gameObject.SetActive(false);

            BuildCompactRoster(font);
            BuildCompactSpMeter(font);
            BuildCompactBattlefield(font);
            BuildCompactSkillPanel(font);
            BuildCompactTimeline(font);
            BuildResultPanel(font);
            compactLayoutBuilt = true;
        }

        private void BuildCompactRoster(Font font)
        {
            Image roster = CreateImage("AllyRoster", backgroundRoot.transform, new Color(Surface.r, Surface.g, Surface.b, 0.76f), false);
            SetMiddleLeft(roster.rectTransform, new Vector2(24f, -80f), new Vector2(190f, 250f));
            Text title = CreateText("RosterTitle", roster.transform, font, 15, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(170f, 20f));
            SetTopLeft(title.rectTransform, new Vector2(10f, -10f), new Vector2(170f, 20f));
            title.text = "아군 편성";
            title.color = Secondary;

            allyRosterButtons = new Button[CombatTrainingBattle.UnitCount];
            allyRosterFrames = new Image[CombatTrainingBattle.UnitCount];
            allyRosterLabels = new Text[CombatTrainingBattle.UnitCount];
            allyRosterOrderLabels = new Text[CombatTrainingBattle.UnitCount];
            for (int index = 0; index < CombatTrainingBattle.UnitCount; index++)
            {
                float y = 180f - index * 63f;
                Button button = CreateButton("AllyRoster_" + index, roster.transform, font, string.Empty, new Vector2(95f, y), new Vector2(180f, 58f), out _);
                Image frame = button.GetComponent<Image>();
                allyRosterButtons[index] = button;
                allyRosterFrames[index] = frame;

                Image selectionRail = CreateImage("SelectionRail", button.transform, SelectedGreen, false);
                SetRect(selectionRail.rectTransform, Vector2.zero, Vector2.zero, new Vector2(5f, 29f), new Vector2(3f, 44f));
                Image portraitPlate = CreateImage("PortraitPlate", button.transform, new Color(PrimaryLight.r, PrimaryLight.g, PrimaryLight.b, 0.9f), false);
                SetRect(portraitPlate.rectTransform, Vector2.zero, Vector2.zero, new Vector2(30f, 29f), new Vector2(42f, 44f));
                Text portraitMark = CreateText("PortraitMark", portraitPlate.transform, font, 12, TextAnchor.MiddleCenter, new Vector2(21f, 22f), new Vector2(36f, 36f));
                portraitMark.text = RosterMark((CombatTrainingUnitId)index);
                portraitMark.color = index == 0 ? Secondary : Gold;
                allyRosterLabels[index] = CreateText("Label", button.transform, font, 13, TextAnchor.MiddleLeft, new Vector2(116f, 38f), new Vector2(116f, 24f));
                allyRosterOrderLabels[index] = CreateText("Order", button.transform, font, 11, TextAnchor.MiddleLeft, new Vector2(116f, 14f), new Vector2(116f, 16f));
            }
        }

        private void EnsureCompactSpMeter(Font font)
        {
            if (spLabel != null)
            {
                // The header is reserved for round and action-line status. Shared SP is the
                // party's common budget, so it is shown immediately below the party roster.
                spLabel.gameObject.SetActive(false);
            }

            if (compactSpMeter != null
                && compactSpValueLabel != null
                && compactSpDeltaLabel != null
                && compactSpStarLabels != null
                && compactSpStarLabels.Length == CombatTrainingBattle.MaximumSp)
            {
                return;
            }

            if (backgroundRoot == null)
            {
                return;
            }

            BuildCompactSpMeter(font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        }

        private void BuildCompactSpMeter(Font font)
        {
            if (compactSpMeter != null)
            {
                return;
            }

            Image meter = CreateImage("SharedSpMeter", backgroundRoot.transform, new Color(Surface.r, Surface.g, Surface.b, 0.84f), false);
            SetMiddleLeft(meter.rectTransform, new Vector2(24f, -252f), new Vector2(190f, 82f));
            compactSpMeter = meter.gameObject;

            Text title = CreateText("SharedSpTitle", meter.transform, font, 13, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(170f, 18f));
            SetTopLeft(title.rectTransform, new Vector2(10f, -7f), new Vector2(170f, 18f));
            title.text = "공유 SP";
            title.color = Secondary;

            compactSpValueLabel = CreateText("SharedSpValue", meter.transform, font, 12, TextAnchor.MiddleRight, Vector2.zero, new Vector2(72f, 18f));
            SetTopRight(compactSpValueLabel.rectTransform, new Vector2(-8f, -7f), new Vector2(72f, 18f));
            compactSpValueLabel.color = MainText;

            compactSpStarLabels = new Text[CombatTrainingBattle.MaximumSp];
            for (int index = 0; index < compactSpStarLabels.Length; index++)
            {
                Text star = CreateText("SharedSpStar_" + index, meter.transform, font, 23, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(19f, 27f));
                SetTopLeft(star.rectTransform, new Vector2(8f + index * 22f, -25f), new Vector2(19f, 27f));
                star.text = "★";
                star.fontStyle = FontStyle.Bold;
                compactSpStarLabels[index] = star;
            }

            compactSpDeltaLabel = CreateText("SharedSpDelta", meter.transform, font, 10, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(174f, 14f));
            SetBottomCenter(compactSpDeltaLabel.rectTransform, new Vector2(0f, 5f), new Vector2(174f, 14f));
            compactSpDeltaLabel.color = MutedText;
        }

        private void RenderCompactSpMeter(CombatTrainingBattle battle)
        {
            if (!compactLayoutBuilt || compactSpMeter == null || compactSpValueLabel == null || compactSpDeltaLabel == null)
            {
                return;
            }

            int projectedSp = battle.SharedSp;
            int plannedCost = 0;
            int plannedGain = 0;
            bool hasPlannedSkill = false;
            bool hasUnaffordableSkill = false;

            // The action line is already speed-sorted. This shows the same SP sequence that
            // the player will see when automatic playback begins.
            for (int timelineIndex = 0; timelineIndex < battle.TimelineEntryCount; timelineIndex++)
            {
                CombatTimelineEntry entry = battle.GetTimelineEntry(timelineIndex);
                if (entry.Side != CombatTimelineSide.Ally || !entry.HasAllyAction || entry.IsCancelled)
                {
                    continue;
                }

                CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(entry.AllyAction.SkillId);
                hasPlannedSkill = true;
                if (projectedSp < skill.SpCost)
                {
                    hasUnaffordableSkill = true;
                    continue;
                }

                plannedCost += skill.SpCost;
                int afterCost = projectedSp - skill.SpCost;
                int actualGain = Mathf.Min(CombatTrainingBattle.MaximumSp, afterCost + skill.SpGain) - afterCost;
                plannedGain += actualGain;
                projectedSp = afterCost + actualGain;
            }

            compactSpValueLabel.text = "SP " + projectedSp + " / " + CombatTrainingBattle.MaximumSp;
            compactSpDeltaLabel.text = hasUnaffordableSkill
                ? "계획 SP 부족"
                : hasPlannedSkill
                    ? "계획  사용 -" + plannedCost + " · 회복 +" + plannedGain
                    : "기술을 고르면 예상 SP를 표시";
            compactSpDeltaLabel.color = hasUnaffordableSkill ? Error : hasPlannedSkill ? MainText : MutedText;

            for (int index = 0; index < compactSpStarLabels.Length; index++)
            {
                Text star = compactSpStarLabels[index];
                if (star == null)
                {
                    continue;
                }

                bool filledAfterPlan = index < projectedSp;
                bool newlyRecovered = filledAfterPlan && index >= battle.SharedSp;
                star.color = filledAfterPlan
                    ? newlyRecovered ? SelectedGreen : Gold
                    : new Color(MutedText.r, MutedText.g, MutedText.b, 0.28f);
            }
        }

        private void EnsureActorPresentation()
        {
            EnsureCompactBattleVisualRoot();
            if (HasActorPresentation() || compactBattleVisualRoot == null)
            {
                return;
            }

            if (actorPresentationRoot != null)
            {
                Destroy(actorPresentationRoot.gameObject);
                actorPresentationRoot = null;
            }

            Sprite[] sprites = new Sprite[ActorPresentationCount];
            for (int index = 0; index < ActorPresentationCount; index++)
            {
                Texture2D texture = Resources.Load<Texture2D>(ActorSpriteResourcePaths[index]);
                if (texture == null)
                {
                    return;
                }

                sprites[index] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            GameObject root = new GameObject("ActorPresentationLayer", typeof(RectTransform));
            root.transform.SetParent(compactBattleVisualRoot, false);
            actorPresentationRoot = root.GetComponent<RectTransform>();
            SetStretch(actorPresentationRoot);
            actorPresentationRoot.SetSiblingIndex(Mathf.Min(lineupImage.transform.GetSiblingIndex() + 1, compactBattleVisualRoot.childCount - 1));

            actorPresentationImages = new Image[ActorPresentationCount];
            actorImpactOverlays = new Image[ActorPresentationCount];
            actorDamagePopups = new Text[ActorPresentationCount];
            Font font = playbackLabel != null && playbackLabel.font != null
                ? playbackLabel.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int index = 0; index < ActorPresentationCount; index++)
            {
                Image actor = CreateImage("ActorPresentation_" + index, actorPresentationRoot, Color.white, false);
                actor.sprite = sprites[index];
                actor.preserveAspect = true;
                actor.raycastTarget = false;
                actorPresentationImages[index] = actor;

                Image impact = CreateImage("ActorImpactFlash_" + index, actor.transform, new Color(Error.r, Error.g, Error.b, 0f), false);
                SetStretch(impact.rectTransform);
                actorImpactOverlays[index] = impact;

                Text damage = CreateText("ActorDamagePopup_" + index, actorPresentationRoot, font, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(100f, 28f));
                damage.color = Error;
                damage.gameObject.SetActive(false);
                actorDamagePopups[index] = damage;
            }

            actorPresentationRoot.gameObject.SetActive(false);
        }

        private void EnsureCompactBattleVisualRoot()
        {
            if (compactBattleVisualRoot != null)
            {
                compactBattleVisualRoot.localScale = Vector3.one * CompactBattleVisualScale;
                return;
            }

            // Existing generated scenes already have the old direct-child layout. Reparent
            // that layout at runtime too, so a code update fixes the presentation immediately
            // without hand-editing a generated Unity scene.
            if (!compactLayoutBuilt || lineupImage == null)
            {
                return;
            }

            RectTransform stage = lineupImage.transform.parent as RectTransform;
            if (stage == null)
            {
                return;
            }

            GameObject root = new GameObject("CompactBattleVisualRoot", typeof(RectTransform));
            root.transform.SetParent(stage, false);
            compactBattleVisualRoot = root.GetComponent<RectTransform>();
            SetCenter(compactBattleVisualRoot, new Vector2(80f, -25f), new Vector2(1320f, 760f));
            compactBattleVisualRoot.localScale = Vector3.one * CompactBattleVisualScale;

            MoveCenteredVisualToRoot(lineupImage.rectTransform);
            MoveCenteredVisualToRoot(playbackLabel != null ? playbackLabel.rectTransform : null);
            MoveCenteredVisualToRoot(feedbackLabel != null ? feedbackLabel.rectTransform : null);

            MoveVisualArrayToRoot(allyFieldAnchors);
            MoveVisualArrayToRoot(allyFieldGlows);
            MoveVisualArrayToRoot(allyFieldLabels);
            MoveVisualArrayToRoot(enemyFieldAnchors);
            MoveVisualArrayToRoot(enemyFieldGlows);
            MoveVisualArrayToRoot(enemyTargetButtons);
            MoveVisualArrayToRoot(enemyFieldLabels);
            MoveVisualArrayToRoot(enemyIntentIconLabels);
            MoveVisualArrayToRoot(enemyIntentLabels);
            MoveVisualArrayToRoot(enemyConditionLabels);

            if (compactArrowLayer != null)
            {
                compactArrowLayer.SetParent(compactBattleVisualRoot, false);
                SetStretch(compactArrowLayer);
            }
        }

        private void MoveCenteredVisualToRoot(RectTransform visual)
        {
            if (visual == null || compactBattleVisualRoot == null || visual.parent == compactBattleVisualRoot)
            {
                return;
            }

            Vector2 localPosition = visual.anchoredPosition - compactBattleVisualRoot.anchoredPosition;
            Vector2 size = visual.sizeDelta;
            visual.SetParent(compactBattleVisualRoot, false);
            SetCenter(visual, localPosition, size);
        }

        private void MoveVisualArrayToRoot(RectTransform[] visuals)
        {
            if (visuals == null)
            {
                return;
            }

            for (int index = 0; index < visuals.Length; index++)
            {
                MoveCenteredVisualToRoot(visuals[index]);
            }
        }

        private void MoveVisualArrayToRoot(Image[] visuals)
        {
            if (visuals == null)
            {
                return;
            }

            for (int index = 0; index < visuals.Length; index++)
            {
                MoveCenteredVisualToRoot(visuals[index] != null ? visuals[index].rectTransform : null);
            }
        }

        private void MoveVisualArrayToRoot(Text[] visuals)
        {
            if (visuals == null)
            {
                return;
            }

            for (int index = 0; index < visuals.Length; index++)
            {
                MoveCenteredVisualToRoot(visuals[index] != null ? visuals[index].rectTransform : null);
            }
        }

        private void MoveVisualArrayToRoot(Button[] visuals)
        {
            if (visuals == null)
            {
                return;
            }

            for (int index = 0; index < visuals.Length; index++)
            {
                MoveCenteredVisualToRoot(visuals[index] != null ? visuals[index].GetComponent<RectTransform>() : null);
            }
        }

        private bool HasActorPresentation()
        {
            return actorPresentationRoot != null
                && actorPresentationImages != null
                && actorPresentationImages.Length == ActorPresentationCount
                && actorImpactOverlays != null
                && actorImpactOverlays.Length == ActorPresentationCount
                && actorDamagePopups != null
                && actorDamagePopups.Length == ActorPresentationCount;
        }

        private void AlignActorPresentationToIllustration()
        {
            if (!HasActorPresentation() || lineupImage == null)
            {
                return;
            }

            for (int index = 0; index < ActorPresentationCount; index++)
            {
                bool ally = index < CombatTrainingBattle.UnitCount;
                int actorIndex = ally ? index : index - CombatTrainingBattle.UnitCount;
                Vector2 actor = ally
                    ? GetLineupStagePosition(AllyLineupPoints[actorIndex])
                    : GetLineupStagePosition(EnemyLineupPoints[actorIndex]);
                Vector2 foot = ally
                    ? GetLineupStagePosition(AllyLineupFootPoints[actorIndex])
                    : GetLineupStagePosition(EnemyLineupFootPoints[actorIndex]);
                Vector2 size = ActorPresentationSizes[index];
                actorPresentationBaseSizes[index] = size;
                actorPresentationBasePositions[index] = new Vector2(actor.x, foot.y + ActorGroundOffsets[index]);
            }
        }

        private void RenderCompactActorPresentation(CombatTrainingBattle battle)
        {
            EnsureActorPresentation();
            if (!HasActorPresentation())
            {
                if (lineupImage != null)
                {
                    lineupImage.color = Color.white;
                }

                return;
            }

            bool playback = battle.Phase == CombatTrainingPhase.Resolving;
            actorPresentationRoot.gameObject.SetActive(playback);
            if (!playback)
            {
                lineupImage.color = Color.white;
                return;
            }

            // One visual set at a time: the composed lineup is for planning, and the six
            // transparent actor assets are for playback. Do not draw both simultaneously.
            lineupImage.color = new Color(1f, 1f, 1f, 0f);
            AlignActorPresentationToIllustration();
            for (int index = 0; index < ActorPresentationCount; index++)
            {
                bool alive = index < CombatTrainingBattle.UnitCount
                    ? battle.IsUnitAlive((CombatTrainingUnitId)index)
                    : battle.IsEnemyAlive((CombatTrainingEnemyId)(index - CombatTrainingBattle.UnitCount));
                actorPresentationAlive[index] = alive;
                actorPresentationImages[index].gameObject.SetActive(alive || actorDefeatPending[index]);
            }

            RefreshActorPresentationAnimation();
        }

        private void BeginActionPresentation(CombatTimelineEntry entry, string message)
        {
            focusedTimelineIndex = entry.TimelineIndex;
            focusedActorPresentationIndex = GetActorPresentationIndex(entry);
            focusStartedAt = Time.unscaledTime;
            impactStartedAt = -1f;
            impactActorCount = 0;
            for (int index = 0; index < actorDamagePopups.Length; index++)
            {
                if (actorDamagePopups[index] != null)
                {
                    actorDamagePopups[index].gameObject.SetActive(false);
                }

                actorDefeatPending[index] = false;
            }

            if (playbackLabel != null)
            {
                playbackLabel.text = message ?? string.Empty;
            }

            ApplyPlaybackFocus();
        }

        private void TriggerImpactPresentation(CombatTimelineEntry entry, CombatTimelineResolution resolution, string message)
        {
            if (focusedTimelineIndex != entry.TimelineIndex)
            {
                BeginActionPresentation(entry, message);
            }
            else if (playbackLabel != null)
            {
                playbackLabel.text = message ?? string.Empty;
            }

            impactStartedAt = Time.unscaledTime;
            impactActorCount = 0;
            if (!resolution.Skipped)
            {
                if (resolution.Side == CombatTimelineSide.Ally)
                {
                    AddImpactActor(CombatTrainingBattle.UnitCount + (int)resolution.TargetEnemyId, resolution.TotalDamage);
                }
                else
                {
                    for (int unitIndex = 0; unitIndex < CombatTrainingBattle.UnitCount; unitIndex++)
                    {
                        int damage = resolution.GetIncomingDamage((CombatTrainingUnitId)unitIndex);
                        if (damage > 0)
                        {
                            AddImpactActor(unitIndex, damage);
                        }
                    }
                }
            }

            ApplyPlaybackFocus();
        }

        private void AddImpactActor(int actorIndex, int damage)
        {
            if (actorIndex < 0 || actorIndex >= ActorPresentationCount || impactActorCount >= impactActorIndices.Length)
            {
                return;
            }

            impactActorIndices[impactActorCount] = actorIndex;
            impactDamageValues[impactActorCount] = damage;
            impactActorCount++;
            actorDefeatPending[actorIndex] = !actorPresentationAlive[actorIndex];
            if (HasActorPresentation())
            {
                actorPresentationImages[actorIndex].gameObject.SetActive(true);
                Text popup = actorDamagePopups[actorIndex];
                popup.text = "-" + damage;
                popup.color = Error;
                popup.gameObject.SetActive(damage > 0);
            }
        }

        private void RefreshActorPresentationAnimation()
        {
            if (!HasActorPresentation())
            {
                return;
            }

            float now = Time.unscaledTime;
            float impactAge = impactStartedAt < 0f ? float.PositiveInfinity : now - impactStartedAt;
            bool focusActive = focusedActorPresentationIndex >= 0 && focusStartedAt >= 0f;
            for (int index = 0; index < ActorPresentationCount; index++)
            {
                Image actor = actorPresentationImages[index];
                if (actor == null)
                {
                    continue;
                }

                bool hit = TryGetImpactActor(index, out int damage);
                bool defeatFading = actorDefeatPending[index] && hit && impactAge < DefeatFadeDuration;
                bool visible = actorPresentationAlive[index] || defeatFading;
                bool focused = focusActive && index == focusedActorPresentationIndex;
                if (!visible)
                {
                    actor.gameObject.SetActive(false);
                    if (actorDamagePopups[index] != null)
                    {
                        actorDamagePopups[index].gameObject.SetActive(false);
                    }

                    continue;
                }

                actor.gameObject.SetActive(true);
                float step = focused ? Mathf.Clamp01((now - focusStartedAt) / 0.14f) : 0f;
                if (focused && impactStartedAt >= focusStartedAt)
                {
                    step *= 1f - Mathf.Clamp01(impactAge / 0.30f);
                }

                Vector2 direction = index < CombatTrainingBattle.UnitCount ? Vector2.right : Vector2.left;
                Vector2 shake = hit && impactAge < ImpactFlashDuration
                    ? Vector2.right * Mathf.Sin(impactAge * 95f) * 5f * (1f - impactAge / ImpactFlashDuration)
                    : Vector2.zero;
                actor.rectTransform.anchoredPosition = actorPresentationBasePositions[index] + direction * (FocusStepDistance * step) + shake;
                actor.rectTransform.sizeDelta = actorPresentationBaseSizes[index];
                actor.rectTransform.localScale = Vector3.one * (focused ? FocusScale : 1f);

                Color color = focused
                    ? Color.white
                    : focusActive
                        ? new Color(0.34f, 0.40f, 0.50f, 0.58f)
                        : Color.white;
                if (hit && impactAge < ImpactFlashDuration)
                {
                    color = Color.Lerp(new Color(1f, 0.24f, 0.24f, 1f), color, impactAge / ImpactFlashDuration);
                }

                if (defeatFading)
                {
                    color.a = 1f - Mathf.Clamp01(impactAge / DefeatFadeDuration);
                }

                actor.color = color;
                Image flash = actorImpactOverlays[index];
                flash.color = hit && impactAge < ImpactFlashDuration
                    ? new Color(1f, 0.16f, 0.16f, 0.48f * (1f - impactAge / ImpactFlashDuration))
                    : new Color(Error.r, Error.g, Error.b, 0f);

                Text popup = actorDamagePopups[index];
                if (popup != null)
                {
                    bool popupVisible = hit && damage > 0 && impactAge < 0.56f;
                    popup.gameObject.SetActive(popupVisible);
                    if (popupVisible)
                    {
                        float rise = Mathf.Clamp01(impactAge / 0.56f) * 34f;
                        SetCenter(popup.rectTransform, actorPresentationBasePositions[index] + new Vector2(0f, actorPresentationBaseSizes[index].y * 0.58f + rise), new Vector2(100f, 28f));
                        Color popupColor = Error;
                        popupColor.a = 1f - Mathf.Clamp01(impactAge / 0.56f);
                        popup.color = popupColor;
                    }
                }
            }
        }

        private bool TryGetImpactActor(int actorIndex, out int damage)
        {
            for (int index = 0; index < impactActorCount; index++)
            {
                if (impactActorIndices[index] == actorIndex)
                {
                    damage = impactDamageValues[index];
                    return true;
                }
            }

            damage = 0;
            return false;
        }

        private static int GetActorPresentationIndex(CombatTimelineEntry entry)
        {
            if (entry.Side == CombatTimelineSide.Enemy)
            {
                return CombatTrainingBattle.UnitCount + (int)entry.EnemyId;
            }

            return entry.HasAllyAction
                ? (int)CombatTrainingBattle.GetSkillDefinition(entry.AllyAction.SkillId).ActorId
                : -1;
        }

        private void BuildCompactBattlefield(Font font)
        {
            GameObject stage = new GameObject("BattleStage", typeof(RectTransform));
            stage.transform.SetParent(backgroundRoot.transform, false);
            SetStretch(stage.GetComponent<RectTransform>());

            // This is a single visual coordinate space: original lineup + hit targets +
            // intent curves + names. Scaling this root keeps every element aligned.
            GameObject visualRoot = new GameObject("CompactBattleVisualRoot", typeof(RectTransform));
            visualRoot.transform.SetParent(stage.transform, false);
            compactBattleVisualRoot = visualRoot.GetComponent<RectTransform>();
            SetCenter(compactBattleVisualRoot, new Vector2(80f, -25f), new Vector2(1320f, 760f));
            compactBattleVisualRoot.localScale = Vector3.one * CompactBattleVisualScale;

            Text allyLabel = CreateText("AllyFieldLabel", stage.transform, font, 15, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(180f, 24f));
            SetTopLeft(allyLabel.rectTransform, new Vector2(24f, -108f), new Vector2(180f, 24f));
            allyLabel.text = "아군";
            allyLabel.color = Secondary;
            Text enemyLabel = CreateText("EnemyFieldLabel", stage.transform, font, 15, TextAnchor.MiddleRight, Vector2.zero, new Vector2(260f, 24f));
            SetTopRight(enemyLabel.rectTransform, new Vector2(-24f, -108f), new Vector2(260f, 24f));
            enemyLabel.text = "적 행동 예고";
            enemyLabel.color = Error;
            Image rule = CreateImage("BattlefieldRule", stage.transform, new Color(Gold.r, Gold.g, Gold.b, 0.35f), false);
            SetHorizontalStretch(rule.rectTransform, 24f, 24f, 920f, 1f);

            lineupImage = CreateImage("CombatIllustrationLineup", compactBattleVisualRoot, Color.white, false);
            // Preserve the full source illustration. The visual root above scales it together
            // with every battle annotation instead of cropping only the characters.
            SetCenter(lineupImage.rectTransform, Vector2.zero, new Vector2(820f, 328f));
            lineupImage.sprite = lineupSprite;
            lineupImage.preserveAspect = true;
            if (lineupSprite == null)
            {
                lineupImage.color = new Color(0f, 0f, 0f, 0f);
                Text fallback = CreateText("IllustrationFallback", compactBattleVisualRoot, font, 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(650f, 32f));
                SetCenter(fallback.rectTransform, Vector2.zero, new Vector2(650f, 32f));
                fallback.text = "전투 일러스트 준비 중";
                fallback.color = MutedText;
            }

            playbackLabel = CreateText("Playback", compactBattleVisualRoot, font, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(640f, 28f));
            SetCenter(playbackLabel.rectTransform, new Vector2(0f, 365f), new Vector2(640f, 28f));
            playbackLabel.color = Gold;
            feedbackLabel = CreateText("Feedback", compactBattleVisualRoot, font, 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(820f, 26f));
            SetCenter(feedbackLabel.rectTransform, new Vector2(0f, 331f), new Vector2(820f, 26f));
            feedbackLabel.color = MainText;

            allyFieldGlows = new Image[CombatTrainingBattle.UnitCount];
            allyFieldAnchors = new RectTransform[CombatTrainingBattle.UnitCount];
            allyFieldLabels = new Text[CombatTrainingBattle.UnitCount];
            Vector2[] allyPositions =
            {
                new Vector2(-292f, -95f),
                new Vector2(-163f, -80f),
                new Vector2(-65f, -90f),
            };
            for (int index = 0; index < CombatTrainingBattle.UnitCount; index++)
            {
                RectTransform anchor = CreateAnchor("AllyAnchor_" + index, compactBattleVisualRoot, new Vector2(0.5f, 0.5f), allyPositions[index]);
                allyFieldAnchors[index] = anchor;
                Image underline = CreateImage("AllyFieldGlow_" + index, compactBattleVisualRoot, new Color(Secondary.r, Secondary.g, Secondary.b, 0.72f), false);
                SetCenter(underline.rectTransform, allyPositions[index] + new Vector2(0f, -145f), new Vector2(76f, 4f));
                allyFieldGlows[index] = underline;
                allyFieldLabels[index] = CreateText("AllyFieldLabel_" + index, compactBattleVisualRoot, font, 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(148f, 42f));
                SetCenter(allyFieldLabels[index].rectTransform, allyPositions[index] + new Vector2(0f, -170f), new Vector2(148f, 42f));
            }

            enemyTargetButtons = new Button[CombatTrainingBattle.EnemyCount];
            enemyFieldGlows = new Image[CombatTrainingBattle.EnemyCount];
            enemyFieldAnchors = new RectTransform[CombatTrainingBattle.EnemyCount];
            enemyFieldLabels = new Text[CombatTrainingBattle.EnemyCount];
            enemyIntentIconLabels = new Text[CombatTrainingBattle.EnemyCount];
            enemyIntentLabels = new Text[CombatTrainingBattle.EnemyCount];
            enemyConditionLabels = new Text[CombatTrainingBattle.EnemyCount];
            Vector2[] enemyPositions =
            {
                new Vector2(215f, -82f),
                new Vector2(350f, -82f),
                new Vector2(475f, -65f),
            };
            for (int index = 0; index < CombatTrainingBattle.EnemyCount; index++)
            {
                RectTransform anchor = CreateAnchor("EnemyAnchor_" + index, compactBattleVisualRoot, new Vector2(0.5f, 0.5f), enemyPositions[index]);
                enemyFieldAnchors[index] = anchor;
                Image underline = CreateImage("EnemyFieldGlow_" + index, compactBattleVisualRoot, new Color(Error.r, Error.g, Error.b, 0.76f), false);
                SetCenter(underline.rectTransform, enemyPositions[index] + new Vector2(0f, -145f), new Vector2(76f, 4f));
                enemyFieldGlows[index] = underline;
                enemyTargetButtons[index] = CreateTransparentButton("EnemyTarget_" + index, compactBattleVisualRoot, Vector2.zero, new Vector2(88f, 188f));
                SetCenter(enemyTargetButtons[index].GetComponent<RectTransform>(), enemyPositions[index], new Vector2(88f, 188f));
                enemyFieldLabels[index] = CreateText("EnemyFieldLabel_" + index, compactBattleVisualRoot, font, 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(148f, 42f));
                SetCenter(enemyFieldLabels[index].rectTransform, enemyPositions[index] + new Vector2(0f, -170f), new Vector2(148f, 42f));
                enemyIntentIconLabels[index] = CreateText("EnemyIntentIcon_" + index, compactBattleVisualRoot, font, 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(28f, 24f));
                SetCenter(enemyIntentIconLabels[index].rectTransform, enemyPositions[index] + new Vector2(0f, 144f), new Vector2(28f, 24f));
                enemyIntentLabels[index] = CreateText("EnemyIntent_" + index, compactBattleVisualRoot, font, 12, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(166f, 28f));
                SetCenter(enemyIntentLabels[index].rectTransform, enemyPositions[index] + new Vector2(0f, 170f), new Vector2(166f, 28f));
                enemyConditionLabels[index] = CreateText("EnemyConditions_" + index, compactBattleVisualRoot, font, 11, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(148f, 20f));
                SetCenter(enemyConditionLabels[index].rectTransform, enemyPositions[index] + new Vector2(0f, -196f), new Vector2(148f, 20f));
                enemyConditionLabels[index].color = Gold;
            }

            GameObject arrowLayer = new GameObject("IntentArrowLayer", typeof(RectTransform));
            arrowLayer.transform.SetParent(compactBattleVisualRoot, false);
            compactArrowLayer = arrowLayer.GetComponent<RectTransform>();
            SetStretch(compactArrowLayer);
            CreateCompactArrowLayer(compactArrowLayer);
            // The arrows intentionally sit over the transparent character lineup. All arrow
            // graphics ignore raycasts, so enemy selection remains available underneath.
            arrowLayer.transform.SetAsLastSibling();
            EnsureActorPresentation();
        }

        private void BuildCompactSkillPanel(Font font)
        {
            skillPanel = CreateImage("SelectedUnitSkillSet", backgroundRoot.transform, new Color(Surface.r, Surface.g, Surface.b, 0.76f), false).gameObject;
            SetMiddleLeft(skillPanel.GetComponent<RectTransform>(), new Vector2(226f, -80f), new Vector2(240f, 250f));
            Text title = CreateText("SkillSetTitle", skillPanel.transform, font, 15, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(216f, 20f));
            SetTopLeft(title.rectTransform, new Vector2(12f, -10f), new Vector2(216f, 20f));
            title.text = "선택 부대 · 기술";
            title.color = Gold;
            selectedUnitLabel = CreateText("SelectedUnit", skillPanel.transform, font, 13, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(216f, 20f));
            SetTopLeft(selectedUnitLabel.rectTransform, new Vector2(12f, -36f), new Vector2(216f, 20f));

            skillButtons = new Button[2];
            skillButtonLabels = new Text[2];
            for (int index = 0; index < 2; index++)
            {
                skillButtons[index] = CreateButton("SkillOption_" + index, skillPanel.transform, font, string.Empty,
                    new Vector2(120f, 154f - index * 70f), new Vector2(224f, 62f), out skillButtonLabels[index]);
                skillButtonLabels[index].fontSize = 12;
                skillButtonLabels[index].alignment = TextAnchor.UpperLeft;
            }
        }

        private void BuildCompactTimeline(Font font)
        {
            Image timeline = CreateImage("IntegratedActionTimeline", backgroundRoot.transform, new Color(Surface.r, Surface.g, Surface.b, 0.72f), false);
            SetBottomStretch(timeline.rectTransform, 24f, 24f, 18f, 156f);
            Text title = CreateText("TimelineTitle", timeline.transform, font, 15, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(330f, 20f));
            SetTopLeft(title.rectTransform, new Vector2(18f, -10f), new Vector2(330f, 20f));
            title.text = "통합 행동선  ·  속도순 실행";
            title.color = Gold;
            executeButton = CreateButton("ExecutePlan", timeline.transform, font, "행동선 확정", Vector2.zero, new Vector2(232f, 32f), out Text executeLabel);
            SetTopRight(executeButton.GetComponent<RectTransform>(), new Vector2(-18f, -8f), new Vector2(232f, 32f));
            executeLabel.fontSize = 14;

            timelineSlotBackgrounds = new Image[MaxTimelineNodeCount];
            timelineSlotLabels = new Text[MaxTimelineNodeCount];
            timelineSpeedChipLabels = new Text[MaxTimelineNodeCount];
            // Action order is derived entirely from initiative; no manual reorder/remove controls.
            for (int timelineIndex = 0; timelineIndex < MaxTimelineNodeCount; timelineIndex++)
            {
                float horizontalAnchor = 0.09f + timelineIndex * 0.164f;
                Image slot = CreateImage("TimelineSlot_" + timelineIndex, timeline.transform,
                    SecondaryDark, false);
                SetAnchoredRect(
                    slot.rectTransform,
                    new Vector2(horizontalAnchor, 0f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 50f),
                    new Vector2(238f, 80f));
                timelineSlotBackgrounds[timelineIndex] = slot;
                Image chip = CreateImage("SpeedChipFrame", slot.transform, new Color(Primary.r, Primary.g, Primary.b, 0.96f), false);
                SetRect(chip.rectTransform, Vector2.zero, Vector2.zero, new Vector2(34f, 67f), new Vector2(50f, 16f));
                timelineSpeedChipLabels[timelineIndex] = CreateText("SpeedChip", chip.transform, font, 10, TextAnchor.MiddleCenter, new Vector2(25f, 9f), new Vector2(46f, 16f));
                timelineSpeedChipLabels[timelineIndex].text = "SPD";
                timelineSpeedChipLabels[timelineIndex].color = Gold;
                timelineSlotLabels[timelineIndex] = CreateText("Label", slot.transform, font, 11, TextAnchor.UpperLeft, new Vector2(128f, 38f), new Vector2(226f, 48f));
            }
        }

        private void BuildRoster(Font font)
        {
            Image roster = CreateImage("AllyRoster", backgroundRoot.transform, new Color(Surface.r, Surface.g, Surface.b, 0.96f), false);
            SetRect(roster.rectTransform, Vector2.zero, Vector2.zero, new Vector2(194f, 563f), new Vector2(350f, 730f));
            Text title = CreateText("RosterTitle", roster.transform, font, 20, TextAnchor.MiddleLeft, new Vector2(30f, 688f), new Vector2(290f, 34f));
            title.text = "아군 부대 · 선택";
            title.color = Secondary;

            allyRosterButtons = new Button[CombatTrainingBattle.UnitCount];
            allyRosterFrames = new Image[CombatTrainingBattle.UnitCount];
            allyRosterLabels = new Text[CombatTrainingBattle.UnitCount];
            allyRosterOrderLabels = new Text[CombatTrainingBattle.UnitCount];
            for (int index = 0; index < CombatTrainingBattle.UnitCount; index++)
            {
                float y = 555f - index * 192f;
                Button button = CreateButton("AllyRoster_" + index, roster.transform, font, string.Empty, new Vector2(175f, y), new Vector2(310f, 168f), out _);
                Image frame = button.GetComponent<Image>();
                allyRosterButtons[index] = button;
                allyRosterFrames[index] = frame;

                Image portraitPlate = CreateImage("PortraitPlate", button.transform, new Color(PrimaryLight.r, PrimaryLight.g, PrimaryLight.b, 0.9f), false);
                SetRect(portraitPlate.rectTransform, Vector2.zero, Vector2.zero, new Vector2(55f, 84f), new Vector2(82f, 128f));
                Text portraitMark = CreateText("PortraitMark", portraitPlate.transform, font, 25, TextAnchor.MiddleCenter, new Vector2(41f, 64f), new Vector2(70f, 96f));
                portraitMark.text = RosterMark((CombatTrainingUnitId)index);
                portraitMark.color = index == 0 ? Secondary : Gold;
                allyRosterLabels[index] = CreateText("Label", button.transform, font, 18, TextAnchor.MiddleLeft, new Vector2(118f, 108f), new Vector2(174f, 74f));
                allyRosterOrderLabels[index] = CreateText("Order", button.transform, font, 16, TextAnchor.MiddleLeft, new Vector2(118f, 43f), new Vector2(174f, 38f));
            }
        }

        private void BuildBattleStage(Font font)
        {
            Image stage = CreateImage("BattleStage", backgroundRoot.transform, new Color(PrimaryLight.r, PrimaryLight.g, PrimaryLight.b, 0.68f), false);
            SetRect(stage.rectTransform, Vector2.zero, Vector2.zero, new Vector2(960f, 620f), new Vector2(1250f, 610f));
            Text allyLabel = CreateText("AllyFieldLabel", stage.transform, font, 18, TextAnchor.MiddleLeft, new Vector2(28f, 576f), new Vector2(420f, 30f));
            allyLabel.text = "아군 · 행동선의 파란 슬롯";
            allyLabel.color = Secondary;
            Text enemyLabel = CreateText("EnemyFieldLabel", stage.transform, font, 18, TextAnchor.MiddleRight, new Vector2(1222f, 576f), new Vector2(430f, 30f));
            enemyLabel.text = "적 · 붉은 화살은 다음 행동";
            enemyLabel.color = Error;

            GameObject arrowLayer = new GameObject("IntentArrowLayer", typeof(RectTransform));
            arrowLayer.transform.SetParent(stage.transform, false);
            SetStretch(arrowLayer.GetComponent<RectTransform>());
            arrowLayer.transform.SetAsFirstSibling();
            CreateArrowLayer(arrowLayer.transform);

            lineupImage = CreateImage("CombatIllustrationLineup", stage.transform, Color.white, false);
            SetRect(lineupImage.rectTransform, Vector2.zero, Vector2.zero, new Vector2(625f, 290f), new Vector2(1230f, 548f));
            lineupImage.sprite = lineupSprite;
            lineupImage.preserveAspect = true;
            if (lineupSprite == null)
            {
                lineupImage.color = new Color(0f, 0f, 0f, 0f);
                Text fallback = CreateText("IllustrationFallback", stage.transform, font, 26, TextAnchor.MiddleCenter, new Vector2(625f, 305f), new Vector2(980f, 90f));
                fallback.text = "전투 일러스트를 불러오는 중";
                fallback.color = MutedText;
            }

            playbackLabel = CreateText("Playback", stage.transform, font, 23, TextAnchor.MiddleCenter, new Vector2(625f, 470f), new Vector2(580f, 60f));
            playbackLabel.color = Gold;
            feedbackLabel = CreateText("Feedback", stage.transform, font, 17, TextAnchor.MiddleCenter, new Vector2(625f, 426f), new Vector2(690f, 54f));
            feedbackLabel.color = MainText;
            instructionLabel = CreateText("Instruction", stage.transform, font, 15, TextAnchor.MiddleCenter, new Vector2(625f, 45f), new Vector2(920f, 40f));
            instructionLabel.color = MutedText;

            allyFieldGlows = new Image[CombatTrainingBattle.UnitCount];
            allyFieldAnchors = new RectTransform[CombatTrainingBattle.UnitCount];
            allyFieldLabels = new Text[CombatTrainingBattle.UnitCount];
            Vector2[] allyPositions =
            {
                new Vector2(82f, 135f),
                new Vector2(290f, 250f),
                new Vector2(455f, 205f),
            };
            Vector2[] allySizes =
            {
                new Vector2(130f, 150f),
                new Vector2(170f, 360f),
                new Vector2(160f, 260f),
            };
            for (int index = 0; index < CombatTrainingBattle.UnitCount; index++)
            {
                Image glow = CreateImage("AllyFieldGlow_" + index, stage.transform, new Color(Secondary.r, Secondary.g, Secondary.b, 0.08f), false);
                SetRect(glow.rectTransform, Vector2.zero, Vector2.zero, allyPositions[index], allySizes[index]);
                allyFieldGlows[index] = glow;
                RectTransform anchor = CreateAnchor("AllyAnchor_" + index, stage.transform, allyPositions[index]);
                allyFieldAnchors[index] = anchor;
                allyFieldLabels[index] = CreateText("AllyFieldLabel_" + index, stage.transform, font, 15, TextAnchor.MiddleCenter,
                    allyPositions[index] + new Vector2(0f, -allySizes[index].y * 0.5f - 28f), new Vector2(180f, 54f));
            }

            enemyTargetButtons = new Button[CombatTrainingBattle.EnemyCount];
            enemyFieldGlows = new Image[CombatTrainingBattle.EnemyCount];
            enemyFieldAnchors = new RectTransform[CombatTrainingBattle.EnemyCount];
            enemyFieldLabels = new Text[CombatTrainingBattle.EnemyCount];
            enemyIntentIconLabels = new Text[CombatTrainingBattle.EnemyCount];
            enemyIntentLabels = new Text[CombatTrainingBattle.EnemyCount];
            enemyConditionLabels = new Text[CombatTrainingBattle.EnemyCount];
            Vector2[] enemyPositions =
            {
                new Vector2(820f, 235f),
                new Vector2(1005f, 245f),
                new Vector2(1170f, 270f),
            };
            Vector2[] enemySizes =
            {
                new Vector2(165f, 300f),
                new Vector2(185f, 330f),
                new Vector2(150f, 330f),
            };
            for (int index = 0; index < CombatTrainingBattle.EnemyCount; index++)
            {
                Image glow = CreateImage("EnemyFieldGlow_" + index, stage.transform, new Color(Danger.r, Danger.g, Danger.b, 0.10f), false);
                SetRect(glow.rectTransform, Vector2.zero, Vector2.zero, enemyPositions[index], enemySizes[index]);
                enemyFieldGlows[index] = glow;
                RectTransform anchor = CreateAnchor("EnemyAnchor_" + index, stage.transform, enemyPositions[index]);
                enemyFieldAnchors[index] = anchor;

                Button hitbox = CreateTransparentButton("EnemyTarget_" + index, stage.transform, enemyPositions[index], enemySizes[index]);
                enemyTargetButtons[index] = hitbox;
                enemyFieldLabels[index] = CreateText("EnemyFieldLabel_" + index, stage.transform, font, 15, TextAnchor.MiddleCenter,
                    enemyPositions[index] + new Vector2(0f, -enemySizes[index].y * 0.5f - 28f), new Vector2(185f, 54f));

                Text intentIcon = CreateText("EnemyIntentIcon_" + index, stage.transform, font, 27, TextAnchor.MiddleCenter,
                    enemyPositions[index] + new Vector2(0f, enemySizes[index].y * 0.5f + 26f), new Vector2(40f, 40f));
                enemyIntentIconLabels[index] = intentIcon;
                enemyIntentLabels[index] = CreateText("EnemyIntent_" + index, stage.transform, font, 14, TextAnchor.MiddleCenter,
                    enemyPositions[index] + new Vector2(0f, enemySizes[index].y * 0.5f + 72f), new Vector2(225f, 64f));
                enemyConditionLabels[index] = CreateText("EnemyConditions_" + index, stage.transform, font, 13, TextAnchor.MiddleCenter,
                    enemyPositions[index] + new Vector2(0f, -enemySizes[index].y * 0.5f - 74f), new Vector2(220f, 30f));
                enemyConditionLabels[index].color = Gold;
            }
        }

        private void BuildSkillPanel(Font font)
        {
            skillPanel = CreateImage("SelectedUnitSkillSet", backgroundRoot.transform, new Color(Surface.r, Surface.g, Surface.b, 0.96f), false).gameObject;
            SetRect(skillPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(1694f, 560f), new Vector2(394f, 720f));
            Text title = CreateText("SkillSetTitle", skillPanel.transform, font, 21, TextAnchor.MiddleLeft, new Vector2(28f, 678f), new Vector2(340f, 32f));
            title.text = "선택 부대 · 기술 세트";
            title.color = Gold;
            selectedUnitLabel = CreateText("SelectedUnit", skillPanel.transform, font, 20, TextAnchor.UpperLeft, new Vector2(28f, 614f), new Vector2(340f, 96f));

            skillButtons = new Button[2];
            skillButtonLabels = new Text[2];
            for (int index = 0; index < 2; index++)
            {
                skillButtons[index] = CreateButton("SkillOption_" + index, skillPanel.transform, font, string.Empty,
                    new Vector2(197f, 430f - index * 224f), new Vector2(344f, 194f), out skillButtonLabels[index]);
                skillButtonLabels[index].fontSize = 16;
                skillButtonLabels[index].alignment = TextAnchor.UpperLeft;
            }

            Text hint = CreateText("SkillHint", skillPanel.transform, font, 14, TextAnchor.UpperLeft, new Vector2(28f, 65f), new Vector2(340f, 88f));
            hint.text = "1. 왼쪽 부대 선택\n2. 적 클릭(파란 표적선)\n3. 여기서 기술 선택\n4. 아래 행동선 순서 조정";
            hint.color = MutedText;
        }

        private void BuildTimeline(Font font)
        {
            Image timeline = CreateImage("IntegratedActionTimeline", backgroundRoot.transform, new Color(Surface.r, Surface.g, Surface.b, 0.98f), false);
            SetRect(timeline.rectTransform, Vector2.zero, Vector2.zero, new Vector2(960f, 154f), new Vector2(1840f, 240f));
            Text title = CreateText("TimelineTitle", timeline.transform, font, 18, TextAnchor.MiddleLeft, new Vector2(28f, 212f), new Vector2(720f, 30f));
            title.text = "통합 행동선 · 속도가 높은 행동부터 실행";
            title.color = Gold;

            timelineSlotBackgrounds = new Image[MaxTimelineNodeCount];
            timelineSlotLabels = new Text[MaxTimelineNodeCount];
            timelineSpeedChipLabels = new Text[MaxTimelineNodeCount];
            for (int timelineIndex = 0; timelineIndex < MaxTimelineNodeCount; timelineIndex++)
            {
                float x = 162f + timelineIndex * 295f;
                Image slot = CreateImage("TimelineSlot_" + timelineIndex, timeline.transform,
                    SecondaryDark, false);
                SetRect(slot.rectTransform, Vector2.zero, Vector2.zero, new Vector2(x, 116f), new Vector2(272f, 164f));
                timelineSlotBackgrounds[timelineIndex] = slot;
                Image chip = CreateImage("SpeedChipFrame", slot.transform, new Color(Primary.r, Primary.g, Primary.b, 0.96f), false);
                SetRect(chip.rectTransform, Vector2.zero, Vector2.zero, new Vector2(34f, 146f), new Vector2(54f, 18f));
                timelineSpeedChipLabels[timelineIndex] = CreateText("SpeedChip", chip.transform, font, 10, TextAnchor.MiddleCenter, new Vector2(27f, 9f), new Vector2(50f, 16f));
                timelineSpeedChipLabels[timelineIndex].color = Gold;
                timelineSlotLabels[timelineIndex] = CreateText("Label", slot.transform, font, 14, TextAnchor.UpperLeft, new Vector2(132f, 92f), new Vector2(244f, 122f));
            }

            executeButton = CreateButton("ExecutePlan", timeline.transform, font, "행동선 확정 · 자동 재생", new Vector2(1640f, 212f), new Vector2(340f, 36f), out _);
            Text footer = CreateText("TimelineFooter", timeline.transform, font, 13, TextAnchor.MiddleLeft, new Vector2(28f, 20f), new Vector2(1440f, 28f));
            footer.text = "앞 기술의 확정 상태와 SP 회복이 뒤 기술의 보너스 확률을 바꿉니다. 확률 실패여도 기본 효과는 항상 적용됩니다.";
            footer.color = MutedText;
        }

        private void BuildResultPanel(Font font)
        {
            resultPanel = CreateImage("Result", backgroundRoot.transform, new Color(Surface.r, Surface.g, Surface.b, 0.99f), true).gameObject;
            SetRect(resultPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 460f));
            resultTitleLabel = CreateText("ResultTitle", resultPanel.transform, font, 40, TextAnchor.MiddleCenter, new Vector2(440f, 354f), new Vector2(760f, 76f));
            resultBodyLabel = CreateText("ResultBody", resultPanel.transform, font, 21, TextAnchor.MiddleCenter, new Vector2(440f, 238f), new Vector2(720f, 126f));
            retryButton = CreateButton("Retry", resultPanel.transform, font, "다시 모의", new Vector2(260f, 80f), new Vector2(280f, 66f), out _);
            resultExitButton = CreateButton("ResultExit", resultPanel.transform, font, "집행관에게 복귀", new Vector2(620f, 80f), new Vector2(300f, 66f), out _);
        }

        private void CreateArrowLayer(Transform parent)
        {
            for (int enemy = 0; enemy < CombatTrainingBattle.EnemyCount; enemy++)
            {
                for (int ally = 0; ally < CombatTrainingBattle.UnitCount; ally++)
                {
                    int arrowIndex = enemy * CombatTrainingBattle.UnitCount + ally;
                    enemyIntentArrowLines[arrowIndex] = CreateImage("EnemyIntentArrow_" + enemy + "_" + ally + "_Line", parent, Error, false);
                    enemyIntentArrowHeads[arrowIndex] = CreateImage("EnemyIntentArrow_" + enemy + "_" + ally + "_Head", parent, Error, false);
                    enemyIntentArrowLines[arrowIndex].gameObject.SetActive(false);
                    enemyIntentArrowHeads[arrowIndex].gameObject.SetActive(false);
                }
            }

            targetPreviewLine = CreateImage("TargetPreviewArrow_Line", parent, Secondary, false);
            targetPreviewHead = CreateImage("TargetPreviewArrow_Head", parent, Secondary, false);
            targetPreviewLine.gameObject.SetActive(false);
            targetPreviewHead.gameObject.SetActive(false);
        }

        private void RenderCompactBattlefield(
            CombatTrainingBattle battle,
            CombatTrainingUnitId selectedUnit,
            CombatTrainingEnemyId selectedTarget,
            bool planning)
        {
            AlignCompactBattlefieldToIllustration();

            for (int index = 0; index < CombatTrainingBattle.UnitCount; index++)
            {
                CombatTrainingUnitId unitId = (CombatTrainingUnitId)index;
                CombatTrainingUnitDefinition unit = CombatTrainingBattle.GetUnitDefinition(unitId);
                int hp = battle.GetAllyHp(unitId);
                bool alive = hp > 0;
                allyFieldGlows[index].color = !alive
                    ? new Color(MutedText.r, MutedText.g, MutedText.b, 0.24f)
                    : unitId == selectedUnit
                        ? SelectedGreen
                        : new Color(Secondary.r, Secondary.g, Secondary.b, 0.68f);
                allyFieldLabels[index].text = Shorten(unit.DisplayName, 10) + "\nHP " + hp + "/" + unit.MaximumHp;
                allyFieldLabels[index].color = alive ? MainText : MutedText;
            }

            for (int index = 0; index < CombatTrainingBattle.EnemyCount; index++)
            {
                CombatTrainingEnemyId enemyId = (CombatTrainingEnemyId)index;
                CombatTrainingEnemyDefinition enemy = CombatTrainingBattle.GetEnemyDefinition(enemyId);
                CombatTrainingEnemyIntent intent = battle.GetEnemyIntent(enemyId);
                int hp = battle.GetEnemyHp(enemyId);
                bool alive = hp > 0;
                bool selected = alive && enemyId == selectedTarget;
                int timelineIndex = FindTimelineIndexForEnemy(battle, enemyId);
                bool cancelled = timelineIndex >= 0 && battle.GetTimelineEntry(timelineIndex).IsCancelled;
                battle.GetProjectedEnemyDamageRange(enemyId, out int minimumDamage, out int maximumDamage, out _);

                enemyFieldGlows[index].color = !alive
                    ? new Color(MutedText.r, MutedText.g, MutedText.b, 0.24f)
                    : selected
                        ? Gold
                        : new Color(Error.r, Error.g, Error.b, 0.75f);
                enemyTargetButtons[index].interactable = planning && alive;
                enemyFieldLabels[index].text = Shorten(enemy.DisplayName, 10) + "\nHP " + hp + "/" + enemy.MaximumHp;
                enemyFieldLabels[index].color = alive ? MainText : MutedText;
                enemyIntentIconLabels[index].text = alive && !cancelled ? "!" : "×";
                enemyIntentIconLabels[index].color = alive && !cancelled ? Error : MutedText;
                enemyIntentLabels[index].text = alive && !cancelled
                    ? Shorten(intent.DisplayName, 12) + " · " + FormatDamageRange(minimumDamage, maximumDamage)
                    : "행동 취소";
                enemyIntentLabels[index].color = alive && !cancelled ? Error : MutedText;
                enemyConditionLabels[index].text = Shorten(ConditionSummary(battle.GetEnemyConditions(enemyId)), 16);
            }

            RenderCompactActorPresentation(battle);
        }

        private void RenderCompactSkillSet(
            CombatTrainingBattle battle,
            CombatTrainingUnitId selectedUnit,
            CombatTrainingEnemyId selectedTarget,
            bool planning)
        {
            CombatTrainingUnitDefinition unit = CombatTrainingBattle.GetUnitDefinition(selectedUnit);
            CombatTrainingEnemyDefinition target = CombatTrainingBattle.GetEnemyDefinition(selectedTarget);
            selectedUnitLabel.text = Shorten(unit.DisplayName, 11) + " → " + Shorten(target.DisplayName, 11);
            selectedUnitLabel.color = battle.IsUnitAlive(selectedUnit) ? SelectedGreen : MutedText;

            int firstSkillIndex = (int)selectedUnit * 2;
            for (int option = 0; option < 2; option++)
            {
                CombatTrainingSkillId skillId = (CombatTrainingSkillId)(firstSkillIndex + option);
                visibleSkillIds[option] = skillId;
                CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(skillId);
                bool assigned = TryGetAssignedSkill(battle, selectedUnit, out CombatTrainingSkillId assignedSkill)
                    && assignedSkill == skillId;
                string sp = skill.SpCost > 0 ? "SP -" + skill.SpCost : "SP +" + skill.SpGain;
                skillButtonLabels[option].text = (assigned ? "[배정] " : string.Empty) + Shorten(skill.DisplayName, 12) + "  " + sp
                    + "\n기본 " + skill.BasePower + " · 보너스 +" + skill.BonusPower + " (" + skill.BaseBonusChance + "%)";
                skillButtons[option].interactable = planning
                    && battle.IsUnitAlive(selectedUnit)
                    && battle.IsEnemyAlive(selectedTarget);
                Image image = skillButtons[option].GetComponent<Image>();
                image.color = assigned
                    ? new Color(Gold.r * 0.34f, Gold.g * 0.31f, Gold.b * 0.20f, 1f)
                    : SecondaryDark;
            }
        }

        private void RenderCompactTimeline(CombatTrainingBattle battle, bool planning)
        {
            for (int index = 0; index < timelineSlotBackgrounds.Length; index++)
            {
                timelineSlotBackgrounds[index].gameObject.SetActive(false);
            }

            int displayIndex = 0;
            for (int timelineIndex = 0; timelineIndex < battle.TimelineEntryCount && displayIndex < timelineSlotBackgrounds.Length; timelineIndex++)
            {

                CombatTimelineEntry entry = battle.GetTimelineEntry(timelineIndex);
                if (!ShouldDisplayTimelineEntry(battle, entry))
                {
                    continue;
                }

                int slotIndex = displayIndex++;
                timelineSlotBackgrounds[slotIndex].gameObject.SetActive(true);
                bool ally = entry.Side == CombatTimelineSide.Ally;
                Color normal = ally ? SecondaryDark : new Color(Danger.r, Danger.g, Danger.b, 0.74f);
                Color color = timelineIndex == focusedTimelineIndex ? Gold : normal;

                timelineSlotBackgrounds[slotIndex].color = color;
                timelineSpeedChipLabels[slotIndex].text = entry.Initiative == int.MinValue
                    ? "SPD --"
                    : "SPD " + entry.Initiative;
                if (ally)
                {
                    int slot = entry.AllySlotIndex;
                    bool occupied = entry.HasAllyAction;

                    if (!occupied)
                    {
                        timelineSlotLabels[timelineIndex].text = (timelineIndex + 1).ToString("00") + " · 아군  행동 대기";
                        timelineSlotLabels[timelineIndex].color = MutedText;
                        continue;
                    }

                    CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(entry.AllyAction.SkillId);
                    CombatTrainingEnemyDefinition target = CombatTrainingBattle.GetEnemyDefinition(entry.AllyAction.TargetEnemyId);
                    CombatActionPreview preview = battle.GetActionPreview(slot);
                    string chance = preview.HasChanceRange
                        ? preview.MinimumBonusChance + "~" + preview.MaximumBonusChance + "%"
                        : preview.MaximumBonusChance + "%";
                    string state = entry.IsCancelled && entry.WasResolved
                        ? " · 취소"
                        : string.Empty;
                    timelineSlotLabels[timelineIndex].text = (timelineIndex + 1).ToString("00") + " · "
                        + Shorten(skill.DisplayName, 13) + " → " + Shorten(target.DisplayName, 8)
                        + "\nSP " + preview.ProjectedSpBefore + "→" + preview.ProjectedSpAfter + " · " + chance + state;
                    timelineSlotLabels[timelineIndex].color = preview.CanPay ? MainText : Error;
                }
                else
                {
                    CombatTrainingEnemyDefinition enemy = CombatTrainingBattle.GetEnemyDefinition(entry.EnemyId);
                    CombatTrainingEnemyIntent intent = entry.EnemyIntent;
                    string target = intent.TargetKind == CombatIntentTargetKind.All
                        ? "전체"
                        : CombatTrainingBattle.GetUnitDefinition(intent.TargetUnitId).DisplayName;
                    string state = entry.IsCancelled && entry.WasResolved ? " · 취소" : string.Empty;
                    timelineSlotLabels[timelineIndex].text = (timelineIndex + 1).ToString("00") + " · 적  "
                        + Shorten(intent.DisplayName, 12) + "\n→ " + Shorten(target, 9) + state;
                    timelineSlotLabels[timelineIndex].color = entry.IsCancelled && entry.WasResolved ? MutedText : MainText;
                }

                // A cancelled/dead action has no visual slot. Copy this surviving action into
                // the next display slot so the action line closes its gaps immediately.
                string label = timelineSlotLabels[timelineIndex].text;
                timelineSlotLabels[slotIndex].text = label.Length > 2
                    ? (slotIndex + 1).ToString("00") + label.Substring(2)
                    : label;
                timelineSlotLabels[slotIndex].color = timelineSlotLabels[timelineIndex].color;
            }

            executeButton.interactable = battle.CanExecutePlan;
        }

        private static bool ShouldDisplayTimelineEntry(CombatTrainingBattle battle, CombatTimelineEntry entry)
        {
            if (entry.Side == CombatTimelineSide.Enemy && !battle.IsEnemyAlive(entry.EnemyId))
            {
                return false;
            }

            if (entry.Side == CombatTimelineSide.Ally
                && entry.HasAllyAction
                && !battle.IsUnitAlive(CombatTrainingBattle.GetSkillDefinition(entry.AllyAction.SkillId).ActorId))
            {
                return false;
            }

            // Empty planning slots are useful while composing a round. Every other
            // cancellation is an action whose actor/target can no longer fight, so it
            // disappears instead of remaining as a dead, muted box.
            return entry.SkipReason == CombatTimelineSkipReason.None
                || entry.SkipReason == CombatTimelineSkipReason.EmptySlot;
        }

        private void RenderCompactArrows(
            CombatTrainingBattle battle,
            CombatTrainingUnitId selectedUnit,
            CombatTrainingEnemyId selectedTarget,
            bool planning)
        {
            for (int enemyIndex = 0; enemyIndex < CombatTrainingBattle.EnemyCount; enemyIndex++)
            {
                CombatTrainingEnemyId enemyId = (CombatTrainingEnemyId)enemyIndex;
                CombatTrainingEnemyIntent intent = battle.GetEnemyIntent(enemyId);
                int timelineIndex = FindTimelineIndexForEnemy(battle, enemyId);
                bool enemyCanAct = timelineIndex >= 0
                    && battle.IsEnemyAlive(enemyId)
                    && !battle.GetTimelineEntry(timelineIndex).IsCancelled;
                for (int allyIndex = 0; allyIndex < CombatTrainingBattle.UnitCount; allyIndex++)
                {
                    bool relevantTarget = intent.TargetKind == CombatIntentTargetKind.All
                        || (int)intent.TargetUnitId == allyIndex;
                    bool active = enemyCanAct && relevantTarget && battle.IsUnitAlive((CombatTrainingUnitId)allyIndex);
                    SetEnemyIntentArcActive(
                        enemyIndex,
                        allyIndex,
                        active,
                        GetCompactArrowPosition(enemyFieldAnchors[enemyIndex]),
                        GetCompactArrowPosition(allyFieldAnchors[allyIndex]),
                        Error);
                }
            }

            for (int allyIndex = 0; allyIndex < CombatTrainingBattle.UnitCount; allyIndex++)
            {
                CombatTrainingUnitId unitId = (CombatTrainingUnitId)allyIndex;
                bool hasPlannedTarget = TryGetPlannedTarget(battle, unitId, out CombatTrainingEnemyId plannedTarget);
                bool active = planning
                    && hasPlannedTarget
                    && battle.IsUnitAlive(unitId)
                    && battle.IsEnemyAlive(plannedTarget);
                Color plannedColor = unitId == selectedUnit
                    ? new Color(Secondary.r, Secondary.g, Secondary.b, 0.90f)
                    : new Color(Secondary.r, Secondary.g, Secondary.b, 0.58f);
                SetPlannedTargetArcActive(
                    allyIndex,
                    active,
                    GetCompactArrowPosition(allyFieldAnchors[allyIndex]),
                    active ? GetCompactArrowPosition(enemyFieldAnchors[(int)plannedTarget]) : Vector2.zero,
                    plannedColor);
            }

            bool targetActive = planning
                && battle.IsUnitAlive(selectedUnit)
                && battle.IsEnemyAlive(selectedTarget);
            SetTargetPreviewArcActive(
                targetActive,
                GetCompactArrowPosition(allyFieldAnchors[(int)selectedUnit]),
                GetCompactArrowPosition(enemyFieldAnchors[(int)selectedTarget]),
                Secondary);
        }

        private void CreateCompactArrowLayer(Transform parent)
        {
            enemyIntentArrowSegments = new Image[IntentArcCount * ArcSegmentCount];
            enemyIntentArrowHeads = new Image[IntentArcCount];
            for (int enemy = 0; enemy < CombatTrainingBattle.EnemyCount; enemy++)
            {
                for (int ally = 0; ally < CombatTrainingBattle.UnitCount; ally++)
                {
                    int arcIndex = enemy * CombatTrainingBattle.UnitCount + ally;
                    int offset = arcIndex * ArcSegmentCount;
                    for (int segment = 0; segment < ArcSegmentCount; segment++)
                    {
                        Image line = CreateImage("EnemyIntentArc_" + enemy + "_" + ally + "_" + segment, parent, Error, false);
                        line.gameObject.SetActive(false);
                        enemyIntentArrowSegments[offset + segment] = line;
                    }

                    Image head = CreateImage("EnemyIntentArc_" + enemy + "_" + ally + "_Head", parent, Error, false);
                    head.gameObject.SetActive(false);
                    enemyIntentArrowHeads[arcIndex] = head;
                }
            }

            plannedTargetArcSegments = new Image[PlannedTargetArcCount * ArcSegmentCount];
            plannedTargetArcHeads = new Image[PlannedTargetArcCount];
            for (int ally = 0; ally < PlannedTargetArcCount; ally++)
            {
                int offset = ally * ArcSegmentCount;
                for (int segment = 0; segment < ArcSegmentCount; segment++)
                {
                    Image line = CreateImage("PlannedTargetArc_" + ally + "_" + segment, parent, Secondary, false);
                    line.gameObject.SetActive(false);
                    plannedTargetArcSegments[offset + segment] = line;
                }

                Image head = CreateImage("PlannedTargetArc_" + ally + "_Head", parent, Secondary, false);
                head.gameObject.SetActive(false);
                plannedTargetArcHeads[ally] = head;
            }

            targetPreviewArcSegments = new Image[ArcSegmentCount];
            for (int segment = 0; segment < ArcSegmentCount; segment++)
            {
                Image line = CreateImage("TargetPreviewArc_" + segment, parent, Secondary, false);
                line.gameObject.SetActive(false);
                targetPreviewArcSegments[segment] = line;
            }

            targetPreviewHead = CreateImage("TargetPreviewArc_Head", parent, Secondary, false);
            targetPreviewHead.gameObject.SetActive(false);
        }

        private void SetEnemyIntentArcActive(
            int enemyIndex,
            int allyIndex,
            bool active,
            Vector2 from,
            Vector2 to,
            Color color)
        {
            int arcIndex = enemyIndex * CombatTrainingBattle.UnitCount + allyIndex;
            int offset = arcIndex * ArcSegmentCount;
            for (int segment = 0; segment < ArcSegmentCount; segment++)
            {
                enemyIntentArrowSegments[offset + segment].gameObject.SetActive(active);
            }

            Image head = enemyIntentArrowHeads[arcIndex];
            head.gameObject.SetActive(active);
            if (active)
            {
                SetBezierArc(enemyIntentArrowSegments, offset, head, from, to, color);
            }
        }

        private void SetTargetPreviewArcActive(bool active, Vector2 from, Vector2 to, Color color)
        {
            for (int segment = 0; segment < targetPreviewArcSegments.Length; segment++)
            {
                targetPreviewArcSegments[segment].gameObject.SetActive(active);
            }

            targetPreviewHead.gameObject.SetActive(active);
            if (active)
            {
                SetBezierArc(targetPreviewArcSegments, 0, targetPreviewHead, from, to, color);
            }
        }

        private void SetPlannedTargetArcActive(
            int allyIndex,
            bool active,
            Vector2 from,
            Vector2 to,
            Color color)
        {
            int offset = allyIndex * ArcSegmentCount;
            for (int segment = 0; segment < ArcSegmentCount; segment++)
            {
                plannedTargetArcSegments[offset + segment].gameObject.SetActive(active);
            }

            Image head = plannedTargetArcHeads[allyIndex];
            head.gameObject.SetActive(active);
            if (active)
            {
                SetBezierArc(plannedTargetArcSegments, offset, head, from, to, color);
            }
        }

        private static void SetBezierArc(Image[] segments, int offset, Image head, Vector2 from, Vector2 to, Color color)
        {
            float horizontalDistance = Mathf.Abs(to.x - from.x);
            float lift = Mathf.Clamp(260f + horizontalDistance * 0.18f, 280f, 410f);
            Vector2 control = (from + to) * 0.5f + Vector2.up * lift;
            Vector2 previous = from;
            for (int segment = 0; segment < ArcSegmentCount; segment++)
            {
                float t = (segment + 1f) / ArcSegmentCount;
                Vector2 next = EvaluateQuadraticBezier(from, control, to, t);
                SetArcSegment(segments[offset + segment].rectTransform, previous, next, color);
                previous = next;
            }

            Vector2 tangent = to - control;
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            RectTransform headRect = head.rectTransform;
            headRect.anchorMin = Vector2.zero;
            headRect.anchorMax = Vector2.zero;
            headRect.pivot = new Vector2(0.5f, 0.5f);
            headRect.anchoredPosition = to;
            headRect.sizeDelta = new Vector2(15f, 15f);
            headRect.localRotation = Quaternion.Euler(0f, 0f, angle - 45f);
            head.color = color;
        }

        private static Vector2 EvaluateQuadraticBezier(Vector2 from, Vector2 control, Vector2 to, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * from + 2f * inverse * t * control + t * t * to;
        }

        private static void SetArcSegment(RectTransform segment, Vector2 from, Vector2 to, Color color)
        {
            Vector2 delta = to - from;
            float distance = delta.magnitude;
            segment.anchorMin = Vector2.zero;
            segment.anchorMax = Vector2.zero;
            segment.pivot = new Vector2(0f, 0.5f);
            segment.anchoredPosition = from;
            segment.sizeDelta = new Vector2(distance, 4f);
            segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Image image = segment.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private static string Shorten(string value, int maximumLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maximumLength)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, Math.Max(1, maximumLength - 1)) + "…";
        }

        private void SetVisible(bool visible)
        {
            if (overlayGroup == null)
            {
                return;
            }

            overlayGroup.alpha = visible ? 1f : 0f;
            overlayGroup.interactable = visible;
            overlayGroup.blocksRaycasts = visible;
            if (!visible && eventSystem != null && eventSystem.currentSelectedGameObject != null
                && eventSystem.currentSelectedGameObject.transform.IsChildOf(transform))
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private static int FindAllyActionSlot(CombatTrainingBattle battle, CombatTrainingUnitId unitId)
        {
            for (int slot = 0; slot < CombatTrainingBattle.AllySlotCount; slot++)
            {
                if (!battle.IsAllySlotOccupied(slot))
                {
                    continue;
                }

                CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(battle.GetPlannedAction(slot).SkillId);
                if (skill.ActorId == unitId)
                {
                    return slot;
                }
            }

            return -1;
        }

        private static bool TryGetAssignedSkill(
            CombatTrainingBattle battle,
            CombatTrainingUnitId unitId,
            out CombatTrainingSkillId skillId)
        {
            int slot = FindAllyActionSlot(battle, unitId);
            if (slot >= 0)
            {
                skillId = battle.GetPlannedAction(slot).SkillId;
                return true;
            }

            skillId = default;
            return false;
        }

        private static bool TryGetPlannedTarget(
            CombatTrainingBattle battle,
            CombatTrainingUnitId unitId,
            out CombatTrainingEnemyId targetEnemyId)
        {
            int slot = FindAllyActionSlot(battle, unitId);
            if (slot >= 0)
            {
                targetEnemyId = battle.GetPlannedAction(slot).TargetEnemyId;
                return true;
            }

            targetEnemyId = default;
            return false;
        }

        private static string PhaseLabel(CombatTrainingPhase phase)
        {
            switch (phase)
            {
                case CombatTrainingPhase.Planning:
                    return "행동선 설계";
                case CombatTrainingPhase.Resolving:
                    return "통합 행동선 자동 재생";
                case CombatTrainingPhase.Victory:
                    return "모의전 완료";
                default:
                    return "전선 붕괴";
            }
        }

        private static string Instruction(CombatTrainingPhase phase)
        {
            switch (phase)
            {
                case CombatTrainingPhase.Planning:
                    return "왼쪽 부대 선택 → 오른쪽 기술 선택 · 적을 클릭하면 파란 표적선이 바뀝니다 · 아래 ‹ ›로 아군 순서를 바꾸세요.";
                case CombatTrainingPhase.Resolving:
                    return "아군과 적은 같은 행동선에서 한 칸씩 행동합니다. 사망한 배우·대상은 그 칸이 취소됩니다.";
                default:
                    return string.Empty;
            }
        }

        private static string ConditionSummary(CombatCondition conditions)
        {
            if (conditions == CombatCondition.None)
            {
                return "상태 없음";
            }

            StringBuilder builder = new StringBuilder();
            AppendCondition(builder, conditions, CombatCondition.Sticky, "점착");
            AppendCondition(builder, conditions, CombatCondition.Exposed, "노출");
            AppendCondition(builder, conditions, CombatCondition.Marked, "표식");
            AppendCondition(builder, conditions, CombatCondition.Suppressed, "억제");
            return builder.ToString();
        }

        private static void AppendCondition(StringBuilder builder, CombatCondition value, CombatCondition condition, string label)
        {
            if ((value & condition) != condition)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(" · ");
            }

            builder.Append(label);
        }

        private static string GuaranteedEffectSummary(CombatTrainingSkillDefinition skill)
        {
            StringBuilder builder = new StringBuilder("피해 ");
            builder.Append(skill.BasePower);
            if (skill.SpGain > 0)
            {
                builder.Append(" · SP+");
                builder.Append(skill.SpGain);
            }

            if ((skill.AppliedCondition & CombatCondition.Sticky) != 0)
            {
                builder.Append(" · 점착");
            }

            if ((skill.AppliedCondition & CombatCondition.Exposed) != 0)
            {
                builder.Append(" · 노출");
            }

            if ((skill.AppliedCondition & CombatCondition.Marked) != 0)
            {
                builder.Append(" · 표식");
            }

            if (skill.BaseSuppression > 0)
            {
                builder.Append(" · 적 피해-");
                builder.Append(skill.BaseSuppression);
            }

            return builder.ToString();
        }

        private static string BonusConditionHint(CombatTrainingSkillId skillId)
        {
            switch (skillId)
            {
                case CombatTrainingSkillId.SlipperyFloor:
                    return "표식 +20%p";
                case CombatTrainingSkillId.ElasticCharge:
                    return "점착 +30%p · 표식 +20%p";
                case CombatTrainingSkillId.GateDelay:
                    return "점착 +25%p";
                case CombatTrainingSkillId.BoneSpear:
                    return "점착 +30%p";
                case CombatTrainingSkillId.CoverAmbush:
                    return "점착 +25%p · 노출 +30%p · 표식 +20%p";
                case CombatTrainingSkillId.StoneVolley:
                    return "노출 +20%p";
                default:
                    return "추가 조건 없음";
            }
        }

        private static string SkipReasonLabel(CombatTimelineSkipReason reason)
        {
            switch (reason)
            {
                case CombatTimelineSkipReason.ActorDefeated:
                    return "행동자 사망";
                case CombatTimelineSkipReason.TargetUnavailable:
                    return "대상 사망";
                case CombatTimelineSkipReason.EnemyDefeated:
                    return "적 처치";
                case CombatTimelineSkipReason.InsufficientSp:
                    return "SP 부족";
                case CombatTimelineSkipReason.EmptySlot:
                    return "미배정";
                default:
                    return "취소";
            }
        }

        private static string FormatDamageRange(int minimumDamage, int maximumDamage)
        {
            return minimumDamage == maximumDamage
                ? minimumDamage.ToString()
                : minimumDamage + "~" + maximumDamage;
        }

        private static string RosterMark(CombatTrainingUnitId unitId)
        {
            switch (unitId)
            {
                case CombatTrainingUnitId.Slime001:
                    return "◉\n001";
                case CombatTrainingUnitId.SkeletonGuard:
                    return "⚔\nBONE";
                default:
                    return "⌁\nGOB";
            }
        }

        private Vector2 GetCompactArrowPosition(RectTransform anchor)
        {
            if (compactArrowLayer == null)
            {
                return anchor.anchoredPosition;
            }

            Vector3 localPosition = compactArrowLayer.InverseTransformPoint(anchor.position);
            return new Vector2(
                localPosition.x + compactArrowLayer.rect.width * 0.5f,
                localPosition.y + compactArrowLayer.rect.height * 0.5f);
        }

        private void AlignCompactBattlefieldToIllustration()
        {
            if (lineupImage == null || lineupSprite == null) return;

            for (int index = 0; index < CombatTrainingBattle.UnitCount; index++)
            {
                Vector2 actor = GetLineupStagePosition(AllyLineupPoints[index]);
                Vector2 foot = GetLineupStagePosition(AllyLineupFootPoints[index]);
                SetCenter(allyFieldAnchors[index], actor, Vector2.zero);
                SetCenter(allyFieldGlows[index].rectTransform, foot + new Vector2(0f, -8f), new Vector2(76f, 4f));
                SetCenter(allyFieldLabels[index].rectTransform, foot + new Vector2(0f, -35f), new Vector2(148f, 42f));
            }

            for (int index = 0; index < CombatTrainingBattle.EnemyCount; index++)
            {
                Vector2 actor = GetLineupStagePosition(EnemyLineupPoints[index]);
                Vector2 foot = GetLineupStagePosition(EnemyLineupFootPoints[index]);
                Vector2 hitboxSize = GetLineupStageSize(EnemyLineupHitboxSizes[index]);
                SetCenter(enemyFieldAnchors[index], actor, Vector2.zero);
                SetCenter(enemyTargetButtons[index].GetComponent<RectTransform>(), actor, hitboxSize);
                SetCenter(enemyFieldGlows[index].rectTransform, foot + new Vector2(0f, -8f), new Vector2(76f, 4f));
                SetCenter(enemyFieldLabels[index].rectTransform, foot + new Vector2(0f, -35f), new Vector2(148f, 42f));
                SetCenter(enemyIntentIconLabels[index].rectTransform, actor + new Vector2(0f, hitboxSize.y * 0.5f + 18f), new Vector2(28f, 24f));
                SetCenter(enemyIntentLabels[index].rectTransform, actor + new Vector2(0f, hitboxSize.y * 0.5f + 43f), new Vector2(166f, 28f));
                SetCenter(enemyConditionLabels[index].rectTransform, foot + new Vector2(0f, -61f), new Vector2(148f, 20f));
            }
        }

        private Vector2 GetLineupStagePosition(Vector2 normalizedPoint)
        {
            RectTransform stage = lineupImage.transform.parent as RectTransform;
            if (stage == null) return Vector2.zero;

            Vector2 contentSize = GetLineupContentSize();
            Vector3 worldPoint = lineupImage.rectTransform.TransformPoint(new Vector3(
                (normalizedPoint.x - 0.5f) * contentSize.x,
                (normalizedPoint.y - 0.5f) * contentSize.y,
                0f));
            Vector3 stagePoint = stage.InverseTransformPoint(worldPoint);
            return new Vector2(stagePoint.x, stagePoint.y);
        }

        private Vector2 GetLineupStageSize(Vector2 normalizedSize)
        {
            Vector2 contentSize = GetLineupContentSize();
            return new Vector2(contentSize.x * normalizedSize.x, contentSize.y * normalizedSize.y);
        }

        private Vector2 GetLineupContentSize()
        {
            Rect rect = lineupImage.rectTransform.rect;
            float sourceAspect = lineupSprite.rect.width / lineupSprite.rect.height;
            float rectAspect = rect.width / rect.height;
            return rectAspect > sourceAspect
                ? new Vector2(rect.height * sourceAspect, rect.height)
                : new Vector2(rect.width, rect.width / sourceAspect);
        }

        private static RectTransform CreateAnchor(string name, Transform parent, Vector2 position)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, position, Vector2.zero);
            return root.GetComponent<RectTransform>();
        }

        private static RectTransform CreateAnchor(string name, Transform parent, Vector2 anchor, Vector2 position)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            SetAnchoredRect(root.GetComponent<RectTransform>(), anchor, new Vector2(0.5f, 0.5f), position, Vector2.zero);
            return root.GetComponent<RectTransform>();
        }

        private static Button CreateTransparentButton(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, position, size);
            Image image = root.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            ConfigureButtonColors(button, image.color);
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Font font,
            string label,
            Vector2 position,
            Vector2 size,
            out Text text)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, position, size);
            Image image = root.GetComponent<Image>();
            image.color = SecondaryDark;
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            ConfigureButtonColors(button, SecondaryDark);
            text = CreateText("Label", root.transform, font, 19, TextAnchor.MiddleCenter, size * 0.5f, size - new Vector2(14f, 10f));
            text.text = label;
            return button;
        }

        private static void ConfigureButtonColors(Button button, Color normal)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = Secondary;
            colors.selectedColor = SelectedGreen;
            colors.pressedColor = Gold;
            colors.disabledColor = new Color(0.12f, 0.15f, 0.18f, 0.72f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Vector2 position,
            Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, position, size);
            Text text = root.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = MainText;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            Shadow shadow = root.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.88f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static void SetLineBetween(RectTransform line, RectTransform head, Vector2 from, Vector2 to, Color color)
        {
            Vector2 delta = to - from;
            float distance = delta.magnitude;
            if (distance <= 0.1f)
            {
                line.gameObject.SetActive(false);
                head.gameObject.SetActive(false);
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            line.anchorMin = Vector2.zero;
            line.anchorMax = Vector2.zero;
            line.pivot = new Vector2(0f, 0.5f);
            line.anchoredPosition = from;
            line.sizeDelta = new Vector2(distance, 4f);
            line.localRotation = Quaternion.Euler(0f, 0f, angle);
            Image lineImage = line.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = color;
            }

            head.anchorMin = Vector2.zero;
            head.anchorMax = Vector2.zero;
            head.pivot = new Vector2(0.5f, 0.5f);
            head.anchoredPosition = to;
            head.sizeDelta = new Vector2(14f, 14f);
            head.localRotation = Quaternion.Euler(0f, 0f, angle - 45f);
            Image headImage = head.GetComponent<Image>();
            if (headImage != null)
            {
                headImage.color = color;
            }
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetAnchoredRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            SetAnchoredRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        }

        private static void SetTopCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            SetAnchoredRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, size);
        }

        private static void SetTopRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            SetAnchoredRect(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), position, size);
        }

        private static void SetBottomCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            SetAnchoredRect(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, size);
        }

        private static void SetMiddleLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            SetAnchoredRect(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, size);
        }

        private static void SetCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            SetAnchoredRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        }

        private static void SetTopStretch(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -height);
            rect.offsetMax = Vector2.zero;
        }

        private static void SetBottomStretch(RectTransform rect, float left, float right, float bottom, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        private static void SetHorizontalStretch(RectTransform rect, float left, float right, float centerY, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, centerY - height * 0.5f);
            rect.offsetMax = new Vector2(-right, centerY + height * 0.5f);
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static Color ColorFromHex(string rgb, float alpha)
        {
            if (!ColorUtility.TryParseHtmlString("#" + rgb, out Color color))
            {
                color = Color.white;
            }

            color.a = alpha;
            return color;
        }
    }
}
