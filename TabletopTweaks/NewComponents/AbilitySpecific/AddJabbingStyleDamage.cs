using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace TabletopTweaks.NewComponents.AbilitySpecific
{
    [TypeId("e363557e8e984621bdd113161793cb06")]
    public class AddJabbingStyleDamage : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RulePrepareDamage>,
        IRulebookHandler<RulePrepareDamage>,
        ISubscriber,
        IInitiatorRulebookSubscriber
    {
        public BlueprintUnitFactReference TargetBuff;
        public BlueprintUnitFactReference TargetMasterBuff;
        public BlueprintUnitFactReference MasterFact;
      
        public void OnEventAboutToTrigger(RulePrepareDamage evt) {
            var weapon = evt.DamageBundle.Weapon;
            var isUnarmedStrike = weapon != null &&
                                  weapon.Blueprint.Type.IsNatural && 
                                  weapon.Blueprint.Type.IsUnarmed;
            
            if (evt.DamageBundle.WeaponDamage == null || evt.DamageBundle.First == null || !isUnarmedStrike ) {
                return;
            }

            var initiatorDesc = evt.Initiator.Descriptor;
            var targetDesc = evt.Target.Descriptor;
            var damage = evt.DamageBundle;

            var dmgMultiplier = initiatorDesc.HasFact(MasterFact) ? 2 : 1;

            if (targetDesc.HasFact(TargetMasterBuff) && targetDesc.HasFact(TargetBuff))
            {
                var addedDamage = damage.First!
                    .CreateTypeDescription()
                    .CreateDamage(new DiceFormula(dmgMultiplier * 2, DiceType.D6), 0);
                    
                addedDamage.CriticalModifier = 1;
                evt.Add(addedDamage);
            }
            else if (targetDesc.HasFact(TargetBuff))
            {
                var addedDamage = damage.First!
                    .CreateTypeDescription()
                    .CreateDamage(new DiceFormula(dmgMultiplier, DiceType.D6), 0);
                addedDamage.CriticalModifier = 1;
                evt.Add(addedDamage);
            }
        }

        public void OnEventDidTrigger(RulePrepareDamage evt) {
        }
    }
}
