using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;
using EloBuddy.SDK.Constants;

namespace Blessed_Riven
{
    class Program
    {
        public static Spell.Active Q = new Spell.Active(SpellSlot.Q, 300);
        public static Spell.Active E = new Spell.Active(SpellSlot.E, 325);
        public static Spell.Skillshot R = new Spell.Skillshot(SpellSlot.R, 900, SkillShotType.Cone, 250, 1600, 45)
        {
            AllowedCollisionCount = int.MaxValue
        };
        public static Spell.Active W
        {
            get
            {
                return new Spell.Active(SpellSlot.W,
                    (uint)
                        (70 + Player.Instance.BoundingRadius +
                         (Player.Instance.HasBuff("RivenFengShuiEngine") ? 195 : 120)));
            }
        }
        public static bool EnableR;
        public static int LastCastQ;
        public static int LastCastW;
        public static int QCount;
        static Spell.Targeted Smite = null;
        public static Menu Menu, FarmingMenu, MiscMenu, DrawMenu, HarassMenu, ComboMenu, SmiteMenu, Skin;
        static Item Healthpot;
        public static SpellSlot SmiteSlot = SpellSlot.Unknown;
        public static SpellSlot IgniteSlot = SpellSlot.Unknown;
        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };
        private static SpellDataInst Flash;
        private static Spell.Targeted _ignite;
        


        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }

        }

        private static string Smitetype
        {
            get
            {
                if (SmiteBlue.Any(i => Item.HasItem(i)))
                    return "s5_summonersmiteplayerganker";

                if (SmiteRed.Any(i => Item.HasItem(i)))
                    return "s5_summonersmiteduel";

                if (SmiteGrey.Any(i => Item.HasItem(i)))
                    return "s5_summonersmitequick";

                if (SmitePurple.Any(i => Item.HasItem(i)))
                    return "itemsmiteaoe";

                return "summonersmite";
            }
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.ChampionName != "Riven")
                return;


            Bootstrap.Init(null);
            SpellDataInst smite = _Player.Spellbook.Spells.Where(spell => spell.Name.Contains("smite")).Any() ? _Player.Spellbook.Spells.Where(spell => spell.Name.Contains("smite")).First() : null;
            if (smite != null)
            {
                Smite = new Spell.Targeted(smite.Slot, 500);
            }
            Healthpot = new Item(2003, 0);
            _ignite = new Spell.Targeted(ObjectManager.Player.GetSpellSlotFromName("summonerdot"), 600);
            Menu = MainMenu.AddMenu("Blessed Riven", "BlessedRiven");
            Flash = ObjectManager.Player.Spellbook.Spells.FirstOrDefault(a => a.Name.ToLower().Contains("summonerflash"));
            Chat.Print("Blessed Riven Loaded.", System.Drawing.Color.Brown);       

            ComboMenu = Menu.AddSubMenu("Combo Settings","ComboSettings");            
            ComboMenu.AddLabel("Combo Settings");
            ComboMenu.Add("QCombo", new CheckBox("Use Q"));
            ComboMenu.Add("WCombo", new CheckBox("Use W"));
            ComboMenu.Add("ECombo", new CheckBox("Use E"));
            ComboMenu.Add("RCombo", new CheckBox("Use R"));            
            ComboMenu.Add("R2Combo", new CheckBox("Use R2(enemy killable)"));
            ComboMenu.Add("FlashBurst", new KeyBind("Burst(Disabled for Now)", false, KeyBind.BindTypes.HoldActive, 'G'));
            ComboMenu.AddLabel("Burst = The flash has uses");
            ComboMenu.AddLabel("If not perform without a flash");
            ComboMenu.Add("ForcedR", new KeyBind("Forced R", false, KeyBind.BindTypes.PressToggle, 'Z'));
            ComboMenu.Add("useTiamat", new CheckBox("Use Items"));
            ComboMenu.AddLabel("R Settings");
            ComboMenu.Add("RCantKill", new CheckBox("Cant Kill with Combo"));
            ComboMenu.Add("REnemyCount", new Slider("Enemy Count > ", 1, 0, 4));

            HarassMenu = Menu.AddSubMenu("Harass Settings", "HarassSettings");
            HarassMenu.AddLabel("Harass Settings");
            HarassMenu.Add("QHarass", new CheckBox("Use Q"));
            HarassMenu.Add("WHarass", new CheckBox("Use W"));
            HarassMenu.Add("EHarass", new CheckBox("Use E"));
            var Style = HarassMenu.Add("harassstyle", new Slider("Harass Style", 0, 0, 2));
            Style.OnValueChange += delegate
            {
                Style.DisplayName = "Harass Style: " + new[] { "Q,Q,W,Q and E back", "E,H,Q3,W", "E,H,AA,Q,W" }[Style.CurrentValue];
            };
            Style.DisplayName = "Harass Style: " + new[] { "Q,Q,W,Q and E back", "E,H,Q3,W", "E,H,AA,Q,W" }[Style.CurrentValue];

            FarmingMenu = Menu.AddSubMenu("Clear Settings", "FarmSettings");
            FarmingMenu.AddLabel("Lane Clear");
            FarmingMenu.Add("QLaneClear", new CheckBox("Use Q LaneClear"));
            FarmingMenu.Add("WLaneClear", new CheckBox("Use W LaneClear"));
            FarmingMenu.Add("ELaneClear", new CheckBox("Use E LaneClear"));

            FarmingMenu.AddLabel("Jungle Clear");
            FarmingMenu.Add("QJungleClear", new CheckBox("Use Q in Jungle"));
            FarmingMenu.Add("WJungleClear", new CheckBox("Use W in Jungle"));
            FarmingMenu.Add("EJungleClear", new CheckBox("Use E in Jungle"));

            FarmingMenu.AddLabel("Last Hit");
            FarmingMenu.Add("Qlasthit", new CheckBox("Use Q LastHit"));
            FarmingMenu.Add("Wlasthit", new CheckBox("Use W LastHit"));
            FarmingMenu.Add("Elasthit", new CheckBox("Use E LastHit"));


            SetSmiteSlot();
            if (SmiteSlot != SpellSlot.Unknown)
            {
                SmiteMenu = Menu.AddSubMenu("Smite Usage", "SmiteUsage");
                SmiteMenu.Add("SmiteEnemy", new CheckBox("Use Smite Combo for Enemy!"));               
                SmiteMenu.AddLabel("Smite Usage");
                SmiteMenu.Add("Use Smite?", new CheckBox("Use Smite"));
                SmiteMenu.Add("Red?", new CheckBox("Red"));
                SmiteMenu.Add("Blue?", new CheckBox("Blue"));
                SmiteMenu.Add("Dragon?", new CheckBox("Dragon"));
                SmiteMenu.Add("Baron?", new CheckBox("Baron"));
            }


            MiscMenu = Menu.AddSubMenu("More Settings", "Misc");
            MiscMenu.AddSeparator();
            MiscMenu.AddLabel("Auto");
            MiscMenu.Add("AutoIgnite", new CheckBox("Auto Ignite"));
            MiscMenu.Add("AutoQSS", new CheckBox("Auto QSS"));
            MiscMenu.Add("AutoW", new CheckBox("Auto W"));
            MiscMenu.AddSeparator();
            MiscMenu.AddLabel("Keep Alive Settings");
            MiscMenu.Add("Alive.Q", new CheckBox("Keep Q Alive"));
            MiscMenu.Add("Alive.R", new CheckBox("Use R2 Before Expire"));
            MiscMenu.AddSeparator();
            MiscMenu.AddLabel("Extra");
            MiscMenu.Add("interrupter", new CheckBox("Use Interruptable Spells"));
            MiscMenu.Add("gapcloser", new CheckBox("Use Gapclose Spells"));
            MiscMenu.AddSeparator();
            MiscMenu.AddLabel("Activator");
            MiscMenu.Add("useHP", new CheckBox("Use Health Potion"));           
            MiscMenu.Add("useHPV", new Slider("HP < %", 45, 0, 100));
            MiscMenu.Add("useElixir", new CheckBox("Use Elixir"));
            MiscMenu.Add("useElixirCount", new Slider("EnemyCount > ", 1, 0, 4));
            MiscMenu.Add("useCrystal", new CheckBox("Use Refillable Potions"));
            MiscMenu.Add("useCrystalHPV", new Slider("HP < %", 65, 0, 100));
            MiscMenu.Add("useCrystalManaV", new Slider("Mana < %", 65, 0, 100));

            Skin = Menu.AddSubMenu("Skin Changer", "SkinChanger");
            Skin.Add("checkSkin", new CheckBox("Use Skin Changer"));
            Skin.Add("skin.Id", new Slider("Skin", 4, 0, 6));

            DrawMenu = Menu.AddSubMenu("Draw Settings", "Drawings");
            DrawMenu.Add("drawStatus", new CheckBox("Draw Status"));
            DrawMenu.Add("drawCombo", new CheckBox("Draw Combo Range"));
            DrawMenu.Add("drawFBurst", new CheckBox("Draw Flash Burst Range"));

            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
            Gapcloser.OnGapcloser += Gapcloser_OnGapCloser;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            
            Game.OnTick += IgniteEvent;
        }
        private static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    _Player.Spellbook.Spells.Where(
                        spell => string.Equals(spell.Name, Smitetype, StringComparison.CurrentCultureIgnoreCase)))
            {
                SmiteSlot = spell.Slot;
            }
        }

        private static void IgniteEvent(EventArgs args)
        {
            if (!_ignite.IsReady() || Player.Instance.IsDead) return;
            if (!MiscMenu["AutoIgnite"].Cast<CheckBox>().CurrentValue)
                return;
            foreach (
                var source in
                    EntityManager.Heroes.Enemies
                        .Where(
                            a => a.IsValidTarget(_ignite.Range) &&
                                a.Health < 50 + 20 * Player.Instance.Level - (a.HPRegenRate / 5 * 3)))
            {
                _ignite.Cast(source);
                return;
            }
        }

        private static void DoQSS()
        {
            if (!MiscMenu["AutoQSS"].Cast<CheckBox>().CurrentValue) return;

            if (Item.HasItem(3139) && Item.CanUseItem(3139) && ObjectManager.Player.CountEnemiesInRange(1800) > 0)
            {
                Core.DelayAction(() => Item.UseItem(3139), 1);
            }

            if (Item.HasItem(3140) && Item.CanUseItem(3140) && ObjectManager.Player.CountEnemiesInRange(1800) > 0)
            {
                Core.DelayAction(() => Item.UseItem(3140), 1);
            }
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender,
            Interrupter.InterruptableSpellEventArgs e)
        {
            
            if (MiscMenu["interrupter"].Cast<CheckBox>().CurrentValue && sender.IsEnemy &&
                e.DangerLevel >= DangerLevel.Medium && sender.IsValidTarget(900))
            {
                E.Cast(sender.ServerPosition);
            }
        }

        public static void Gapcloser_OnGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (MiscMenu["gapcloser"].Cast<CheckBox>().CurrentValue && sender.IsEnemy &&
                sender.IsValidTarget(900))
            {
                E.Cast(sender.ServerPosition);
            }
        }

        private static void Game_OnTick(EventArgs args)
        {
            var HPpot = MiscMenu["useHP"].Cast<CheckBox>().CurrentValue;       
            var HPv = MiscMenu["useHPv"].Cast<Slider>().CurrentValue;
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var w = TargetSelector.GetTarget(W.Range, DamageType.Physical);

            if(w.IsValidTarget(W.Range) && MiscMenu["AutoW"].Cast<CheckBox>().CurrentValue)
            {
                W.Cast();
            }

            if(_Player.HasBuffOfType(BuffType.Stun) || _Player.HasBuffOfType(BuffType.Taunt) || _Player.HasBuffOfType(BuffType.Polymorph) || _Player.HasBuffOfType(BuffType.Frenzy) || _Player.HasBuffOfType(BuffType.Fear) || _Player.HasBuffOfType(BuffType.Snare) || _Player.HasBuffOfType(BuffType.Suppression))
            {
                DoQSS();
            }

            if (Smite != null)
            {
                if (Smite.IsReady() && SmiteMenu["Use Smite?"].Cast<CheckBox>().CurrentValue)
                {
                    Obj_AI_Minion Mob = EntityManager.MinionsAndMonsters.GetJungleMonsters(_Player.Position, Smite.Range).FirstOrDefault();

                    if (Mob != default(Obj_AI_Minion))
                    {
                        bool kill = GetSmiteDamage() >= Mob.Health;

                        if (kill)
                        {
                            if ((Mob.Name.Contains("SRU_Dragon") || Mob.Name.Contains("SRU_Baron"))) Smite.Cast(Mob);
                            else if (Mob.Name.StartsWith("SRU_Red") && SmiteMenu["Red?"].Cast<CheckBox>().CurrentValue) Smite.Cast(Mob);
                            else if (Mob.Name.StartsWith("SRU_Blue") && SmiteMenu["Blue?"].Cast<CheckBox>().CurrentValue) Smite.Cast(Mob);
                        }
                    }
                }
            }

            if (LastCastQ + 3600 < Environment.TickCount)
            {
                QCount = 0;
            }
            if (MiscMenu["Alive.Q"].Cast<CheckBox>().CurrentValue && !Player.Instance.IsRecalling() && QCount < 3 && QCount > 0 && LastCastQ + 3480 < Environment.TickCount && Player.Instance.HasBuff("RivenTriCleaveBuff"))
            {
                Player.CastSpell(SpellSlot.Q,
                    Orbwalker.LastTarget != null && Orbwalker.LastAutoAttack - Environment.TickCount < 3000
                        ? Orbwalker.LastTarget.Position
                        : Game.CursorPos);
            }

            if (HPpot && Player.Instance.HealthPercent < HPv)
            {
                if (Item.HasItem(Healthpot.Id) && Item.CanUseItem(Healthpot.Id) && !Player.HasBuff("RegenerationPotion"))
                {
                    Healthpot.Cast();
                }
            }


            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
                SmiteOnTarget(t);
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JungleClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                Flee();
            }
        }

        private static void Burst()
        {
            var target = TargetSelector.SelectedTarget;
            if (target == null || !target.IsValidTarget()) return;
            Orbwalker.ForcedTarget = target;
            Orbwalker.OrbwalkTo(target.ServerPosition);
            if (target.IsValidTarget() && !target.IsZombie)
            {
                if (R.IsReady() && R.Name == "RivenFengShuiEngine" && W.IsReady() && E.IsReady() &&
                    _Player.Distance(target.Position) <= 250 + 70 + _Player.AttackRange)
                {
                    Player.CastSpell(SpellSlot.E, target.Position);
                    if (Item.HasItem(3142) && Item.CanUseItem(3142))
                    {
                        Item.UseItem(3142);
                    }
                    Core.DelayAction(ForceW, 100);
                    Player.CastSpell(SpellSlot.Q, target.Position);
                }
                else if (R.IsReady() && R.Name == "RivenFengShuiEngine" && E.IsReady() && W.IsReady() && Q.IsReady() &&
                         _Player.Distance(target.Position) <= 400 + 70 + _Player.AttackRange)
                {
                    if (Item.HasItem(3142) && Item.CanUseItem(3142))
                    {
                        Item.UseItem(3142);
                    }
                    Player.CastSpell(SpellSlot.E, target.Position);
                    Player.CastSpell(SpellSlot.Q, target.Position);
                    Core.DelayAction(ForceW, 160);
                }
                else if (Flash.IsReady
                         && R.IsReady() && R.Name == "RivenFengShuiEngine" && (_Player.Distance(target.Position) <= 800) &&
                         ( (Item.HasItem(3074) && Item.CanUseItem(3074))))

                {
                    Player.CastSpell(SpellSlot.E, target.Position);
                    if (Item.HasItem(3142) && Item.CanUseItem(3142))
                    {
                        Item.UseItem(3142);
                    }
                    Core.DelayAction(FlashW, 180);
                    Player.CastSpell(SpellSlot.Q, target.Position);
                }
                else if (Flash.IsReady
                         && R.IsReady() && E.IsReady() && W.IsReady() && R.Name == "RivenFengShuiEngine" &&
                         (_Player.Distance(target.Position) <= 800) && Item.HasItem(3074) && Item.CanUseItem(3074))
                {
                    Player.CastSpell(SpellSlot.E, target.Position);
                    Core.DelayAction(ForceItem, 100);
                    Core.DelayAction(FlashW, 210);
                    Player.CastSpell(SpellSlot.Q, target.Position);
                }
            }
        }

        private static void FlashW()
        {
            var target = TargetSelector.SelectedTarget;
            if (target != null && target.IsValidTarget() && !target.IsZombie)
            {
                W.Cast();
                Core.DelayAction(() => _Player.Spellbook.CastSpell(Flash.Slot, target.Position), 1);
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

            if (args.SData.Name.ToLower().Contains(W.Name.ToLower()))
            {
                LastCastW = Environment.TickCount;
                return;
            }
            if (args.SData.Name.ToLower().Contains(Q.Name.ToLower()))
            {
                LastCastQ = Environment.TickCount;
                if (!MiscMenu["Alive.Q"].Cast<CheckBox>().CurrentValue) return;
                Core.DelayAction(() =>
                {
                    if (!Player.Instance.IsRecalling() && QCount < 2)
                    {
                        Player.CastSpell(SpellSlot.Q,
                            Orbwalker.LastTarget != null && Orbwalker.LastAutoAttack - Environment.TickCount < 3000
                                ? Orbwalker.LastTarget.Position
                                : Game.CursorPos);
                    }
                }, 3480);
                return;
            }
        }

        private static void ForceW()
        {
            return;
        }

        private static void ForceItem()
        {
            if (Item.HasItem(3074) && Item.CanUseItem(3074))
            {
                Item.UseItem(3074);
            }
            else if (Item.HasItem(3077) && Item.CanUseItem(3077))
            {
                Item.UseItem(3077);
            }
        }

        private static void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe) return;
            var t = 0;
            switch (args.Animation)
            {
                case "Spell1a":
                    t = 251;
                    QCount = 1;
                    break;
                case "Spell1b":
                    t = 251;
                    QCount = 2;
                    break;
                case "Spell1c":
                    t = 343;
                    QCount = 0;
                    break;
                case "Spell2":
                    t = 120;
                    break;
                case "Spell3":
                    break;
                case "Spell4a":
                    t = 0;
                    break;
                case "Spell4b":
                    t = 100;
                    break;
            }
            if (t != 0 && (Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None))
            {
                Orbwalker.ResetAutoAttack();
                Core.DelayAction(CancelAnimation, t - Game.Ping);
            }
        }

        private static void CancelAnimation()
        {
            Player.DoEmote(Emote.Dance);
            Orbwalker.ResetAutoAttack();
        }

        private static void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            var target = args.Target as Obj_AI_Base;

            // Hydra
            if (args.SData.Name.ToLower().Contains("itemtiamatcleave"))
            {
                Orbwalker.ResetAutoAttack();
                if (W.IsReady())
                {
                    var target2 = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                    if (target2 != null || Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None)
                    {
                        Player.CastSpell(SpellSlot.W);
                    }
                }
                return;
            }

            //W
            if (args.SData.Name.ToLower().Contains(W.Name.ToLower()))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (Player.Instance.HasBuff("RivenFengShuiEngine") && R.IsReady() &&
                        ComboMenu["R2Combo"].Cast<CheckBox>().CurrentValue)
                    {
                        var target2 = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                        if (target2 != null &&
                            (target2.Distance(Player.Instance) < W.Range &&
                             target2.Health >
                             Player.Instance.CalculateDamageOnUnit(target2, DamageType.Physical, Damage.WDamage()) ||
                             target2.Distance(Player.Instance) > W.Range) &&
                            Player.Instance.CalculateDamageOnUnit(target2, DamageType.Physical,
                                Damage.RDamage(target2) + Damage.WDamage()) > target2.Health)
                        {
                            R.Cast(target2);
                        }
                    }
                }

                target = (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) ||
                          Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                    ? TargetSelector.GetTarget(E.Range + W.Range, DamageType.Physical)
                    : (Obj_AI_Base)Orbwalker.LastTarget;
                if (Q.IsReady() && Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None && target != null)
                {
                    Player.CastSpell(SpellSlot.Q, target.Position);
                    return;
                }
                return;
            }

            //E
            if (args.SData.Name.ToLower().Contains(E.Name.ToLower()))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (Player.Instance.HasBuff("RivenFengShuiEngine") && R.IsReady() &&
                        ComboMenu["R2Combo"].Cast<CheckBox>().CurrentValue)
                    {
                        var target2 = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                        if (target2 != null &&
                            Player.Instance.CalculateDamageOnUnit(target2, DamageType.Physical,
                                (Damage.RDamage(target2))) > target2.Health)
                        {
                            R.Cast(target2);
                            return;
                        }
                    }
                    if ((EnableR == true) && R.IsReady() &&
                        !Player.Instance.HasBuff("RivenFengShuiEngine") &&
                        ComboMenu["RCombo"].Cast<CheckBox>().CurrentValue)
                    {
                        Player.CastSpell(SpellSlot.R);
                    }
                    target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                    if (target != null && Player.Instance.Distance(target) < W.Range)
                    {
                        Player.CastSpell(SpellSlot.W);
                        return;
                    }
                }
            }

            //Q
            if (args.SData.Name.ToLower().Contains(Q.Name.ToLower()))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (Player.Instance.HasBuff("RivenFengShuiEngine") && R.IsReady() &&
                        ComboMenu["R2Combo"].Cast<CheckBox>().CurrentValue)
                    {
                        var target2 = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                        if (target2 != null &&
                            (target2.Distance(Player.Instance) < 300 &&
                             target2.Health >
                             Player.Instance.CalculateDamageOnUnit(target2, DamageType.Physical, Damage.QDamage()) ||
                             target2.Distance(Player.Instance) > 300) &&
                            Player.Instance.CalculateDamageOnUnit(target2, DamageType.Physical,
                                Damage.RDamage(target2) + Damage.QDamage()) > target2.Health)
                        {
                            R.Cast(target2);
                        }
                    }
                }
                return;
            }

            if (args.SData.IsAutoAttack() && target != null)
            {                               
                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                        {
                            ComboAfterAa(target);
                        }

                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                        {
                            HarassAfterAa(target);
                        }

                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) &&
                            target.IsMinion())
                        {
                            JungleAfterAa(target);
                        }

                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) && target.IsMinion())
                        {
                            LastHitAfterAa(target);
                        }

                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && target.IsMinion())
                        {
                            LaneClearAfterAa(target);
                        }      
            }
        }

        public static void ComboAfterAa(Obj_AI_Base target)
        {
            if (Player.Instance.HasBuff("RivenFengShuiEngine") && R.IsReady() &&
                ComboMenu["R2Combo"].Cast<CheckBox>().CurrentValue)
            {
                if (Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, Damage.RDamage(target)) + Player.Instance.GetAutoAttackDamage(target, true) > target.Health)
                {
                    R.Cast(target);
                    return;
                }
            }
            if (ComboMenu["WCombo"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                W.IsInRange(target))
            {
                if (Item.HasItem(3074) && Item.CanUseItem(3074))
                {
                    Item.UseItem(3074);
                    return;
                }
                else if (Item.HasItem(3077) && Item.CanUseItem(3077))
                {
                    Item.UseItem(3077);
                    return;
                }
                Player.CastSpell(SpellSlot.W);
                return;
            }
            if (ComboMenu["QCombo"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                Player.CastSpell(SpellSlot.Q, target.Position);
                return;
            }
            if (Item.HasItem(3074) && Item.CanUseItem(3074))
            {
                Item.UseItem(3074);
            }
            else if (Item.HasItem(3077) && Item.CanUseItem(3077))
            {
                Item.UseItem(3077);
            }
        }

        public static void HarassAfterAa(Obj_AI_Base target)
        {
            if (HarassMenu["WHarass"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                W.IsInRange(target))
            {
                if (Item.HasItem(3074) && Item.CanUseItem(3074))
                {
                    Item.UseItem(3074);
                    return;
                }
                else if (Item.HasItem(3077) && Item.CanUseItem(3077))
                {
                    Item.UseItem(3077);
                    return;
                }
                Player.CastSpell(SpellSlot.W);
                return;
            }
            if (HarassMenu["QHarass"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                Player.CastSpell(SpellSlot.Q, target.Position);
                return;
            }
            if (Item.HasItem(3074) && Item.CanUseItem(3074))
            {
                Item.UseItem(3074);
            }
            else if (Item.HasItem(3077) && Item.CanUseItem(3077))
            {
                Item.UseItem(3077);
            }
        }

        public static void LastHitAfterAa(Obj_AI_Base target)
        {
            var unitHp = target.Health - Player.Instance.GetAutoAttackDamage(target, true);
            if (unitHp > 0)
            {
                if (FarmingMenu["QLastHit"].Cast<CheckBox>().CurrentValue && Q.IsReady() &&
                    Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, Damage.QDamage()) >
                    unitHp)
                {
                    Player.CastSpell(SpellSlot.Q, target.Position);
                    return;
                }
                if (FarmingMenu["WLastHit"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                    W.IsInRange(target) &&
                    Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, Damage.WDamage()) >
                    unitHp)
                {
                    Player.CastSpell(SpellSlot.W);
                }
            }
        }

        public static void LaneClearAfterAa(Obj_AI_Base target)
        {
            var unitHp = target.Health - Player.Instance.GetAutoAttackDamage(target, true);
            if (unitHp > 0)
            {
                if (FarmingMenu["QLaneClear"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    Player.CastSpell(SpellSlot.Q, target.Position);
                    return;
                }
                if (FarmingMenu["WLaneClear"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                    W.IsInRange(target))
                {
                    Player.CastSpell(SpellSlot.W);
                    return;
                }
            }
            else
            {
                List<Obj_AI_Minion> minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                    Player.Instance.Position, Q.Range).Where(a => a.NetworkId != target.NetworkId).ToList();
                if (FarmingMenu["QLaneClear"].Cast<CheckBox>().CurrentValue && Q.IsReady() && minions.Any())
                {
                    Player.CastSpell(SpellSlot.Q, minions[0].Position);
                    return;
                }
                minions = minions.Where(a => a.Distance(Player.Instance) < W.Range).ToList();
                if (FarmingMenu["WLaneClear"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                    W.IsInRange(target) && minions.Any())
                {
                    Player.CastSpell(SpellSlot.W);
                    return;
                }
            }
        }

        public static void JungleAfterAa(Obj_AI_Base target)
        {
            if (FarmingMenu["WJungleClear"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                W.IsInRange(target))
            {
                if (Item.HasItem(3074) && Item.CanUseItem(3074))
                {
                    Item.UseItem(3074);
                    return;
                }
                else if (Item.HasItem(3077) && Item.CanUseItem(3077))
                {
                    Item.UseItem(3077);
                    return;
                }
                Player.CastSpell(SpellSlot.W);
                return;
            }
            if (FarmingMenu["QJungleClear"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                Player.CastSpell(SpellSlot.Q, target.Position);
                return;
            }
            if (Item.HasItem(3074) && Item.CanUseItem(3074))
            {
                Item.UseItem(3074);
            }
            else if (Item.HasItem(3077) && Item.CanUseItem(3077))
            {
                Item.UseItem(3077);
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (checkSkin())
            {
                Player.SetSkinId(SkinId());
            }
        }

        public static int SkinId()
        {
            return Skin["skin.Id"].Cast<Slider>().CurrentValue;
        }
        public static bool checkSkin()
        {
            return Skin["checkSkin"].Cast<CheckBox>().CurrentValue;
        }

        private static void SmiteOnTarget(AIHeroClient t)
        {
            var range = 700f;
            var use = SmiteMenu["SmiteEnemy"].Cast<CheckBox>().CurrentValue;
            var itemCheck = SmiteBlue.Any(i => Item.HasItem(i)) || SmiteRed.Any(i => Item.HasItem(i));
            if (itemCheck && use &&
                _Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready &&
                t.Distance(_Player.Position) < range)
            {
                _Player.Spellbook.CastSpell(SmiteSlot, t);
            }
        }
        private static void Combo()
        {
            if (Orbwalker.IsAutoAttacking) return;
            var target = TargetSelector.GetTarget(E.Range + W.Range + 200, DamageType.Physical);
            var useQ = ComboMenu["QCombo"].Cast<CheckBox>().CurrentValue;
            var useW = ComboMenu["WCombo"].Cast<CheckBox>().CurrentValue;
            var useE = ComboMenu["ECombo"].Cast<CheckBox>().CurrentValue;
            var useR = ComboMenu["RCombo"].Cast<CheckBox>().CurrentValue;
            var useItem = ComboMenu["useTiamat"].Cast<CheckBox>().CurrentValue;
            EnableR = false;
            if (R.IsReady() && Player.Instance.HasBuff("RivenFengShuiEngine") &&
                 ComboMenu["R2Combo"].Cast<CheckBox>().CurrentValue)
            {
                if (
                    EntityManager.Heroes.Enemies.Where(
                        enemy =>
                            enemy.IsValidTarget(R.Range) &&
                            enemy.Health <
                            Player.Instance.CalculateDamageOnUnit(enemy, DamageType.Physical,
                                Damage.RDamage(enemy))).Any(enemy => R.Cast(enemy)))
                {
                    return;
                }
            }

            if (target == null) return;

            if (ComboMenu["RCombo"].Cast<CheckBox>().CurrentValue && R.IsReady() && !Player.Instance.HasBuff("RivenFengShuiEngine"))
            {
                if (ComboMenu["RCantKill"].Cast<CheckBox>().CurrentValue &&
                    target.Health > Damage.ComboDamage(target, true)
                    && target.Health < Damage.ComboDamage(target)
                    && target.Health > Player.Instance.GetAutoAttackDamage(target, true) * 2 ||
                    ComboMenu["REnemyCount"].Cast<Slider>().CurrentValue > 0 &&
                    Player.Instance.CountEnemiesInRange(600) > 1 || IsRActive)
                {
                    if (E.IsReady())
                    {
                        EnableR = true;
                    }
                }
            }

            if (ComboMenu["ECombo"].Cast<CheckBox>().CurrentValue && (target.Distance(Player.Instance) > W.Range || IsRActive && R.IsReady()) && E.IsReady())
            {
                if (Item.HasItem(3142) && Item.CanUseItem(3142))
                {
                    Item.UseItem(3142);
                }
                Player.CastSpell(SpellSlot.E, target.Position);
                return;
            }

            if (ComboMenu["WCombo"].Cast<CheckBox>().CurrentValue &&
                target.Distance(Player.Instance) <= W.Range && W.IsReady())
            {
                if (Item.HasItem(3074) && Item.CanUseItem(3074))
                {
                    Item.UseItem(3074);
                    return;
                }
                else if (Item.HasItem(3077) && Item.CanUseItem(3077))
                {
                    Item.UseItem(3077);
                    return;
                }
                Player.CastSpell(SpellSlot.W);
            }
        }
        private static void Flee()
        {
            var x = _Player.Position.Extend(Game.CursorPos, 300);
            if (Q.IsReady() && !_Player.IsDashing()) Player.CastSpell(SpellSlot.Q, Game.CursorPos);
            if (E.IsReady() && !_Player.IsDashing()) Player.CastSpell(SpellSlot.E, x.To3D());
        }

        public static void Harass()
        {
            if (Orbwalker.IsAutoAttacking) return;

            var target = TargetSelector.GetTarget(E.Range + W.Range, DamageType.Physical);

            if (target == null) return;

            if (HarassMenu["EHarass"].Cast<CheckBox>().CurrentValue &&
                (target.Distance(Player.Instance) > W.Range &&
                 target.Distance(Player.Instance) < E.Range + W.Range ||
                 IsRActive && R.IsReady() &&
                 target.Distance(Player.Instance) < E.Range + 265 + Player.Instance.BoundingRadius) &&
                E.IsReady())
            {
                Player.CastSpell(SpellSlot.E, target.Position);
                return;
            }

            if (HarassMenu["WHarass"].Cast<CheckBox>().CurrentValue &&
                target.Distance(Player.Instance) <= W.Range && W.IsReady())
            {
                if (Item.HasItem(3074) && Item.CanUseItem(3074))
                {
                    Item.UseItem(3074);
                    return;
                }
                else if (Item.HasItem(3077) && Item.CanUseItem(3077))
                {
                    Item.UseItem(3077);
                    return;
                }
                Player.CastSpell(SpellSlot.W);
            }
        }

        private static void JungleClear()
        {
            var minion =
                 EntityManager.MinionsAndMonsters.Monsters.OrderByDescending(a => a.MaxHealth)
                     .FirstOrDefault(a => a.Distance(Player.Instance) < Player.Instance.GetAutoAttackRange(a));

            if (minion == null) return;

            if (FarmingMenu["QJungleClear"].Cast<CheckBox>().CurrentValue && Q.IsReady() &&
                   minion.Health <=
                   Player.Instance.CalculateDamageOnUnit(minion, DamageType.Physical, Damage.QDamage()))
            {
                Player.CastSpell(SpellSlot.Q, minion.Position);
                return;
            }

            if (FarmingMenu["EJungleClear"].Cast<CheckBox>().CurrentValue && (!W.IsReady() && !Q.IsReady() || Player.Instance.HealthPercent < 20) && E.IsReady() &&
                LastCastW + 750 < Environment.TickCount)
            {
                Player.CastSpell(SpellSlot.E, minion.Position);
            }
        }
        public static void LaneClear()
        {
            Orbwalker.ForcedTarget = null;
            if (Orbwalker.IsAutoAttacking || LastCastQ + 260 > Environment.TickCount) return;
            foreach (
                var minion in EntityManager.MinionsAndMonsters.EnemyMinions.Where(a => a.IsValidTarget(W.Range)))
            {
                if (FarmingMenu["QLaneClear"].Cast<CheckBox>().CurrentValue && Q.IsReady() &&
                    minion.Health <=
                    Player.Instance.CalculateDamageOnUnit(minion, DamageType.Physical, Damage.QDamage()))
                {
                    Player.CastSpell(SpellSlot.Q, minion.Position);
                    return;
                }
                if (FarmingMenu["WLaneClear"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                    minion.Health <=
                    Player.Instance.CalculateDamageOnUnit(minion, DamageType.Physical, Damage.WDamage()))
                {
                    Player.CastSpell(SpellSlot.W);
                    return;
                }
            }

        }

            static float GetSmiteDamage()
        {
            float damage = new float();

            if (_Player.Level < 10) damage = 360 + (_Player.Level - 1) * 30;

            else if (_Player.Level < 15) damage = 280 + (_Player.Level - 1) * 40;

            else if (_Player.Level < 19) damage = 150 + (_Player.Level - 1) * 50;

            return damage;
        }

        public static void LastHit()
        {
            Orbwalker.ForcedTarget = null;
            if (Orbwalker.IsAutoAttacking) return;

            foreach (
                var minion in EntityManager.MinionsAndMonsters.EnemyMinions.Where(a => a.IsValidTarget(W.Range)))
            {
                if (FarmingMenu["QLastHit"].Cast<CheckBox>().CurrentValue && Q.IsReady() &&
                    minion.Health <=
                    Player.Instance.CalculateDamageOnUnit(minion, DamageType.Physical, Damage.QDamage()))
                {
                    Player.CastSpell(SpellSlot.Q, minion.Position);
                    return;
                }
                if (FarmingMenu["WLastHit"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                    minion.Health <=
                    Player.Instance.CalculateDamageOnUnit(minion, DamageType.Physical, Damage.WDamage()))
                {
                    Player.CastSpell(SpellSlot.W);
                    return;
                }
            }
        }
        public static bool IsRActive
        {
            get
            {
                return ComboMenu["ForcedR"].Cast<KeyBind>().CurrentValue &&
                       ComboMenu["RCombo"].Cast<CheckBox>().CurrentValue;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (DrawMenu["drawCombo"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawCircle(_Player.Position, Q.Range + E.Range, System.Drawing.Color.Red);
            }
            if (DrawMenu["drawFBurst"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawCircle(_Player.Position, 425 + E.Range, System.Drawing.Color.Green);
            }
            if (DrawMenu["drawStatus"].Cast<CheckBox>().CurrentValue)
            {                
                var pos = Drawing.WorldToScreen(Player.Instance.Position);
                EloBuddy.Drawing.DrawText((int)pos.X - 45, (int)pos.Y + 40, Color.DarkGray, "Forced R: " + IsRActive);
            }
        }
    }
}