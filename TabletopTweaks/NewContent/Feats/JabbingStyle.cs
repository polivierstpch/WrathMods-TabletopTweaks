using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using System;
using TabletopTweaks.Config;
using TabletopTweaks.Extensions;
using TabletopTweaks.NewComponents.AbilitySpecific;
using TabletopTweaks.Utilities;

namespace TabletopTweaks.NewContent.Feats {
    class JabbingStyle {
        public static void AddJabbingStyle() {
            var ImprovedUnarmedStrike = Resources.GetBlueprint<BlueprintFeature>("7812ad3672a4b9a4fb894ea402095167");
            var Dodge = Resources.GetBlueprint<BlueprintFeature>("97e216dbb46ae3c4faef90cf6bbe6fd5");
            var Mobility = Resources.GetBlueprint<BlueprintFeature>("2a6091b97ad940943b46262600eaeaeb");
            var PowerAttack = Resources.GetBlueprint<BlueprintFeature>("9972f33f977fc724c838e59641b2fca5");
            var MonkClass = Resources.GetBlueprint<BlueprintCharacterClass>("e8f21e5b58e0569468e420ebea456124");

            var JabbingStyleIcon = AssetLoader.LoadInternal("Feats", "Icon_JabbingStyle.png");
            var JabbingDancerIcon = AssetLoader.LoadInternal("Feats", "Icon_JabbingDancer.png");
            var JabbingMasterIcon = AssetLoader.LoadInternal("Feats", "Icon_JabbingMaster.png");
            
            var JabbingDancerBuff = Helpers.CreateBuff("JabbingDancerBuff", bp => {
                bp.m_Icon = JabbingDancerIcon;
                bp.SetName("Jabbing Dancer");
                bp.SetDescription("If you hit an opponent with an unarmed strike while using Jabbing Style," +
                                  " you receive a +10 bonus to your Mobility skill checks until the end of the round.");
                bp.AddComponent<AddStatBonus>(c => {
                    c.Stat = StatType.SkillMobility;
                    c.Descriptor = ModifierDescriptor.UntypedStackable;
                    c.Value = 10;
                });
                bp.Ranks = 1;
                bp.IsClassFeature = true;
            });

            var JabbingDancer = Helpers.CreateBlueprint<BlueprintFeature>("JabbingDancer", bp => {
                bp.m_Icon = JabbingDancerIcon;
                bp.SetName(JabbingDancerBuff.Name);
                bp.SetDescription(JabbingDancerBuff.Description);
                bp.Groups = new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat,FeatureGroup.StyleFeat };
                bp.AddPrerequisiteFeature(ImprovedUnarmedStrike);
                bp.AddPrerequisiteFeature(Dodge);
                bp.AddPrerequisiteFeature(Mobility);
                bp.AddPrerequisite<PrerequisiteStatValue>(p => {
                    p.Stat = StatType.BaseAttackBonus;
                    p.Value = 9;
                    p.Group = Prerequisite.GroupType.Any;
                });
                bp.AddPrerequisite<PrerequisiteClassLevel>(p => {
                    p.m_CharacterClass = MonkClass.ToReference<BlueprintCharacterClassReference>();
                    p.Level = 5;
                    p.Group = Prerequisite.GroupType.Any;
                });
            });

            var JabbingDancerBuffApply = Helpers.Create<Conditional>(c => {
                c.ConditionsChecker = new ConditionsChecker {
                    Conditions = new Condition[] {
                        new ContextConditionCasterHasFact {
                            m_Fact = JabbingDancer.ToReference<BlueprintUnitFactReference>(),
                        }
                    }
                };
                c.IfTrue = Helpers.CreateActionList(
                    Helpers.Create<ContextActionApplyBuff>(a => {
                        a.m_Buff = JabbingDancerBuff.ToReference<BlueprintBuffReference>();
                        a.ToCaster = true;
                        a.DurationValue = new ContextDurationValue {
                            BonusValue = 1,
                            DiceCountValue = 0,
                            DiceType = DiceType.Zero,
                            Rate = DurationRate.Rounds
                        };
                        a.UseDurationSeconds = false;
                        a.DurationSeconds = 0f;
                        a.IsNotDispelable = true;
                        a.IsFromSpell = false;
                        a.Permanent = false;
                        a.AsChild = false;
                    })
                );
                c.IfFalse = Helpers.CreateActionList();
            });

            var JabbingMasterTargetBuff = Helpers.CreateBuff("JabbingMasterTargetBuff", bp => {
                bp.m_Icon = JabbingMasterIcon;
                bp.SetName("Jabbing Master Target");
                bp.SetDescription("While using Jabbing Style, the extra damage you deal when you hit a single target with two unarmed strikes increases to 2d6," +
                                  " and the extra damage when you hit a single target with three or more unarmed strikes increases to 4d6.");
                bp.Ranks = 1;
                bp.IsClassFeature = true;
                bp.Stacking = StackingType.Stack;
            });

            var JabbingStyleTargetBuff = Helpers.CreateBuff("JabbingStyleTargetBuff", bp => {
                bp.m_Icon = JabbingStyleIcon;
                bp.SetName("Jabbing Style Target");
                bp.SetDescription("When you hit a target with an unarmed strike and you have hit that target with an unarmed strike previously that round," +
                                  " you deal an extra 1d6 points of damage to that target.");
                bp.Ranks = 1;
                bp.IsClassFeature = true;
                bp.Stacking = StackingType.Stack;
                bp.AddComponent<AddFactContextActions>(c => {
                    c.Activated = Helpers.CreateActionList();
                    c.NewRound = Helpers.CreateActionList();
                    c.Deactivated = Helpers.CreateActionList(
                        Helpers.Create<ContextActionRemoveBuff>(a => {
                            a.m_Buff = JabbingMasterTargetBuff.ToReference<BlueprintBuffReference>();
                            a.ToCaster = true;
                        })
                    );
                });
            });

            var JabbingStyleTargetBuffApply = Helpers.Create<Conditional>(c => {
                c.ConditionsChecker = new ConditionsChecker {
                    Conditions = new Condition[] {
                        new ContextConditionHasBuffFromCaster {
                            m_Buff = JabbingStyleTargetBuff.ToReference<BlueprintBuffReference>(),
                            Not = true
                        }
                    }
                };
                c.IfTrue = Helpers.CreateActionList(
                        Helpers.Create<ContextActionApplyBuff>(a => {
                            a.m_Buff = JabbingStyleTargetBuff.ToReference<BlueprintBuffReference>();
                            a.DurationValue = new ContextDurationValue {
                                m_IsExtendable = false,
                                BonusValue = 1,
                                DiceCountValue = 0,
                                DiceType = DiceType.Zero,
                                Rate = DurationRate.Rounds
                            };
                            a.ToCaster = false;
                            a.UseDurationSeconds = false;
                            a.DurationSeconds = 0f;
                            a.IsNotDispelable = true;
                            a.IsFromSpell = false;
                            a.Permanent = false;
                            a.AsChild = false;
                        })
                    );
                c.IfFalse = Helpers.CreateActionList();
            });

            var JabbingMaster = Helpers.CreateBlueprint<BlueprintFeature>("JabbingMaster", bp => {
                bp.m_Icon = JabbingMasterIcon;
                bp.SetName("Jabbing Master");
                bp.SetDescription(JabbingMasterTargetBuff.Description);
                bp.Groups = new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat,FeatureGroup.StyleFeat };
                bp.AddPrerequisiteFeature(ImprovedUnarmedStrike);
                bp.AddPrerequisiteFeature(Dodge);
                bp.AddPrerequisiteFeature(Mobility);
                bp.AddPrerequisiteFeature(PowerAttack);
                bp.AddPrerequisite<PrerequisiteStatValue>(p => {
                    p.Stat = StatType.BaseAttackBonus;
                    p.Value = 12;
                    p.Group = Prerequisite.GroupType.Any;
                });
                bp.AddPrerequisite<PrerequisiteClassLevel>(p => {
                    p.m_CharacterClass = MonkClass.ToReference<BlueprintCharacterClassReference>();
                    p.Level = 8;
                    p.Group = Prerequisite.GroupType.Any;
                });
            });

            var JabbingMasterTargetBuffApply = Helpers.Create<Conditional>(c => {
                c.ConditionsChecker = new ConditionsChecker {
                    Conditions = new Condition[] {
                        new ContextConditionCasterHasFact {
                            m_Fact = JabbingMaster.ToReference<BlueprintUnitFactReference>(),
                            Not = false
                        },
                        new ContextConditionHasBuffFromCaster {
                            m_Buff = JabbingStyleTargetBuff.ToReference<BlueprintBuffReference>(),
                            Not = false
                        },
                        new ContextConditionHasBuffFromCaster {
                            m_Buff = JabbingMasterTargetBuff.ToReference<BlueprintBuffReference>(),
                            Not = true
                        }
                    }
                };
                c.IfTrue = Helpers.CreateActionList(
                        Helpers.Create<ContextActionApplyBuff>(a => {
                            a.m_Buff = JabbingMasterTargetBuff.ToReference<BlueprintBuffReference>();
                            a.DurationValue = new ContextDurationValue {
                                BonusValue = 1,
                                DiceCountValue = 0,
                                DiceType = DiceType.Zero,
                                m_IsExtendable = false,
                                Rate = DurationRate.Rounds
                            };
                            a.ToCaster = false;
                            a.UseDurationSeconds = false;
                            a.DurationSeconds = 0f;
                            a.IsNotDispelable = true;
                            a.IsFromSpell = false;
                            a.Permanent = false;
                            a.AsChild = false;
                        })
                    );
                c.IfFalse = Helpers.CreateActionList();
            });
            
            var JabbingStyleBuff = Helpers.CreateBuff("JabbingStyleBuff", b => {
                b.m_Icon = JabbingStyleIcon;
                b.SetName("Jabbing Style");
                b.SetDescription(JabbingStyleTargetBuff.Description);
                b.AddComponent<AddJabbingStyleDamage>(c => {
                    c.MasterFact = JabbingMaster.ToReference<BlueprintUnitFactReference>();
                    c.TargetBuff = JabbingStyleTargetBuff.ToReference<BlueprintUnitFactReference>();
                    c.TargetMasterBuff = JabbingMasterTargetBuff.ToReference<BlueprintUnitFactReference>();
                });
                b.AddComponent<AddInitiatorAttackWithWeaponTrigger>(c => {
                    c.Action = Helpers.CreateActionList(
                            JabbingMasterTargetBuffApply, 
                            JabbingStyleTargetBuffApply, 
                            JabbingDancerBuffApply
                        );
                    c.OnlyHit = true;
                    c.RangeType = WeaponRangeType.Melee;
                    c.WaitForAttackResolve = true;
                    c.CheckWeaponRangeType = true;
                });
            });

            var JabbingStyleAbility = Helpers.CreateBlueprint<BlueprintActivatableAbility>("JabbingStyleAbility", bp => {
                bp.m_Icon = JabbingStyleIcon;
                bp.SetName(JabbingStyleBuff.Name);
                bp.SetDescription(JabbingStyleBuff.Description);
                bp.m_Buff = JabbingStyleBuff.ToReference<BlueprintBuffReference>();
                bp.ActivationType = AbilityActivationType.Immediately;
                bp.m_ActivateWithUnitCommand = UnitCommand.CommandType.Free;
                bp.Group = ActivatableAbilityGroup.CombatStyle;
                bp.ResourceAssetIds = Array.Empty<string>();
                bp.DoNotTurnOffOnRest = true; 
            });

            var JabbingStyle = Helpers.CreateBlueprint<BlueprintFeature>("JabbingStyle", bp => {
                bp.m_Icon = JabbingStyleIcon;
                bp.SetName(JabbingStyleBuff.Name);
                bp.SetDescription(JabbingStyleBuff.Description);
                bp.Groups = new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat, FeatureGroup.StyleFeat };
                bp.AddPrerequisiteFeature(ImprovedUnarmedStrike);
                bp.AddPrerequisite<PrerequisiteStatValue>(p => {
                    p.Stat = StatType.BaseAttackBonus;
                    p.Value = 6;
                    p.Group = Prerequisite.GroupType.Any;
                });
                bp.AddPrerequisite<PrerequisiteClassLevel>(p => {
                    p.m_CharacterClass = MonkClass.ToReference<BlueprintCharacterClassReference>();
                    p.Level = 1;
                    p.Group = Prerequisite.GroupType.Any;
                });
                bp.AddComponent<AddFacts>(c => {
                    c.m_Facts = new[] { JabbingStyleAbility.ToReference<BlueprintUnitFactReference>() };
                    c.DoNotRestoreMissingFacts = true;
                });
            });
            
            JabbingDancer.AddPrerequisiteFeature(JabbingStyle);
            JabbingMaster.AddPrerequisiteFeature(JabbingStyle);
            JabbingMaster.AddPrerequisiteFeature(JabbingDancer);
            
            if (ModSettings.AddedContent.Feats.IsDisabled("JabbingStyle")) { return; }
            FeatTools.AddAsFeat(JabbingStyle);
            FeatTools.AddAsFeat(JabbingDancer);
            FeatTools.AddAsFeat(JabbingMaster);
        }
    }
}