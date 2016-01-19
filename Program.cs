using System;
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
using SharpDX;

namespace Blessed_Riven
{
    internal class Program
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
        public static Menu Menu, FarmingMenu, MiscMenu, DrawMenu, HarassMenu, ComboMenu, SmiteMenu, Skin, ShieldMenu, DelayMenu;
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
            Flash = ObjectManager.Player.Spellbook.Spells.FirstOrDefault(a => a.Name.ToLower().Contains("summonerflash"));

            Chat.Print("Blessed Riven Loaded.", System.Drawing.Color.Brown);
            Menu = MainMenu.AddMenu("Blessed Riven", "BlessedRiven");
            ComboMenu = Menu.AddSubMenu("Combo Settings", "ComboSettings");
            ComboMenu.AddLabel("Combo Settings");
            ComboMenu.Add("QCombo", new CheckBox("Use Q"));
            ComboMenu.Add("WCombo", new CheckBox("Use W"));
            ComboMenu.Add("ECombo", new CheckBox("Use E"));
            ComboMenu.Add("RCombo", new CheckBox("Use R"));
            ComboMenu.Add("R2Combo", new CheckBox("Use R2(enemy killable)"));
            ComboMenu.Add("FlashW", new KeyBind("Flash W", false, KeyBind.BindTypes.HoldActive, '5'));
            ComboMenu.Add("FlashBurst", new KeyBind("Burst(broken)", false, KeyBind.BindTypes.HoldActive, 'G'));
            ComboMenu.AddLabel("Burst = Select Target And Burst Key");
            ComboMenu.AddLabel("The flash has usesh");
            ComboMenu.AddLabel("If not perform without a flash");
            ComboMenu.Add("ForcedR", new KeyBind("Forced R", true, KeyBind.BindTypes.PressToggle, 'Z'));
            ComboMenu.Add("useTiamat", new CheckBox("Use Items"));
            ComboMenu.AddLabel("R Settings");
            ComboMenu.Add("RCantKill", new CheckBox("Cant Kill with Combo", false));
            ComboMenu.Add("REnemyCount", new Slider("Enemy Count >= ", 0, 0, 4));

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
            MiscMenu.AddLabel("Auto");
            MiscMenu.Add("AutoIgnite", new CheckBox("Auto Ignite"));
            MiscMenu.Add("AutoQSS", new CheckBox("Auto QSS"));
            MiscMenu.Add("AutoW", new CheckBox("Auto W"));
            MiscMenu.AddLabel("Keep Alive Settings");
            MiscMenu.Add("Alive.Q", new CheckBox("Keep Q Alive"));
            MiscMenu.Add("Alive.R", new CheckBox("Use R2 Before Expire"));
            MiscMenu.AddLabel("Extra");
            MiscMenu.Add("interrupter", new CheckBox("Use Interruptable Spells"));
            MiscMenu.Add("gapcloser", new CheckBox("Use Gapclose Spells"));
            MiscMenu.AddLabel("Activator");
            MiscMenu.Add("useHP", new CheckBox("Use Health Potion"));
            MiscMenu.Add("useHPV", new Slider("HP < %", 45, 0, 100));
            MiscMenu.Add("useElixir", new CheckBox("Use Elixir"));
            MiscMenu.Add("useElixirCount", new Slider("EnemyCount > ", 1, 0, 4));
            MiscMenu.Add("useCrystal", new CheckBox("Use Refillable Potions"));
            MiscMenu.Add("useCrystalHPV", new Slider("HP < %", 65, 0, 100));
            MiscMenu.Add("useCrystalManaV", new Slider("Mana < %", 65, 0, 100));

            DelayMenu = Menu.AddSubMenu("Delay Settings(Humanizer)", "Delay");
            DelayMenu.Add("spell1a1b", new Slider("Q1,Q2 Delay(ms)", 261, 100, 400));
            DelayMenu.Add("spell1c", new Slider("Q3 Delay(ms)", 353, 100, 400));
            DelayMenu.Add("spell2", new Slider("W Delay(ms)", 120, 100, 400));
            DelayMenu.Add("spell4a", new Slider("R Delay(ms)", 0, 0, 400));
            DelayMenu.Add("spell4b", new Slider("R2 Delay(ms)", 100, 50, 400));

            ShieldMenu = Menu.AddSubMenu("Shield Settings", "ShieldSettings");
            ShieldMenu.Add("UseShield", new CheckBox("Use Shield(E)"));

            Skin = Menu.AddSubMenu("Skin Changer", "SkinChanger");
            Skin.Add("checkSkin", new CheckBox("Use Skin Changer"));
            Skin.Add("skin.Id", new Slider("Skin", 4, 0, 6));

            DrawMenu = Menu.AddSubMenu("Draw Settings", "Drawings");
            DrawMenu.Add("drawStatus", new CheckBox("Draw Status"));
            DrawMenu.Add("drawCombo", new CheckBox("Draw Combo Range"));
            DrawMenu.Add("drawFBurst", new CheckBox("Draw Flash Burst Range"));
            DrawMenu.Add("DrawDamage", new CheckBox("Draw Damage Bar"));

            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Game.OnUpdate += OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
            Gapcloser.OnGapcloser += Gapcloser_OnGapCloser;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            getDB();
            Game.OnTick += IgniteEvent;
        }
        private static HashSet<string> DB { get; set; }
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

        private static void getDB()
        {
            if (!ShieldMenu["UseShield"].Cast<CheckBox>().CurrentValue) return;
            DB = new HashSet<string>
            {
                "AhriSeduce"
                ,
                "InfernalGuardian"
                ,
                "EnchantedCrystalArrow"
                ,
                "InfernalGuardian"
                ,
                "EnchantedCrystalArrow"
                ,
                "RocketGrab"
                ,
                "BraumQ"
                ,
                "CassiopeiaPetrifyingGaze"
                ,
                "DariusAxeGrabCone"
                ,
                "DravenDoubleShot"
                ,
                "DravenRCast"
                ,
                "Dazzle"
                ,
                "EzrealTrueshotBarrage"
                ,
                "FizzMarinerDoom"
                ,
                "GnarBigW"
                ,
                "GnarR"
                ,
                "GragasR"
                ,
                "GravesChargeShot"
                ,
                "GravesClusterShot"
                ,
                "JarvanIVDemacianStandard"
                ,
                "JinxW"
                ,
                "JinxR"
                ,
                "KarmaQ"
                ,
                "KogMawLivingArtillery"
                ,
                "LeblancSlide"
                ,
                "LeblancSoulShackle"
                ,
                "LeonaSolarFlare"
                ,
                "LuxLightBinding"
                ,
                "LuxLightStrikeKugel"
                ,
                "LuxMaliceCannon"
                ,
                "UFSlash"
                ,
                "DarkBindingMissile"
                ,
                "NamiQ"
                ,
                "NamiR"
                ,
                "OrianaDetonateCommand"
                ,
                "RengarE"
                ,
                "rivenizunablade"
                ,
                "RumbleCarpetBombM"
                ,
                "SejuaniGlacialPrisonStart"
                ,
                "SionR"
                ,
                "ShenShadowDash"
                ,
                "SonaR"
                ,
                "StaticField"
                ,
                "ThreshQ"
                ,
                "JaxCounterStrike"
                ,
                "VarusQMissilee"
                ,
                "VarusR"
                ,
                "VeigarBalefulStrike"
                ,
                "VelkozQ"
                ,
                "Vi-q"
                ,
                "Laser"
                ,
                "xeratharcanopulse2"
                ,
                "XerathArcaneBarrage2"
                ,
                "XerathMageSpear"
                ,
                "xerathrmissilewrapper"
                ,
                "yasuoq3w"
                ,
                "ZacQ"
                ,
                "ZedShuriken"
                ,
                "ZiggsQ"
                ,
                "ZiggsW"
                ,
                "ZiggsE"
                ,
                "ZiggsR"
                ,
                "ZileanQ"
                ,
                "ZyraQFissure"
                ,
                "ZyraGraspingRoots"
                ,
                "goldcardlock"
            };
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
            try {
                if (MiscMenu["interrupter"].Cast<CheckBox>().CurrentValue && sender.IsEnemy &&
                    e.DangerLevel >= DangerLevel.Medium && sender.IsValidTarget(900))
                {
                    E.Cast(sender.ServerPosition);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

            }
        }

        public static void Gapcloser_OnGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            try {
                if (MiscMenu["gapcloser"].Cast<CheckBox>().CurrentValue && sender.IsEnemy &&
                    sender.IsValidTarget(900))
                {
                    E.Cast(sender.ServerPosition);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

            }
        }

        private static void Game_OnTick(EventArgs args)
        {
            var HPpot = MiscMenu["useHP"].Cast<CheckBox>().CurrentValue;
            var HPv = MiscMenu["useHPv"].Cast<Slider>().CurrentValue;
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var w = TargetSelector.GetTarget(W.Range, DamageType.Physical);

            if (w.IsValidTarget(W.Range) && MiscMenu["AutoW"].Cast<CheckBox>().CurrentValue)
            {
                W.Cast();
            }

            if (_Player.HasBuffOfType(BuffType.Stun) || _Player.HasBuffOfType(BuffType.Taunt) || _Player.HasBuffOfType(BuffType.Polymorph) || _Player.HasBuffOfType(BuffType.Frenzy) || _Player.HasBuffOfType(BuffType.Fear) || _Player.HasBuffOfType(BuffType.Snare) || _Player.HasBuffOfType(BuffType.Suppression))
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
            if (MiscMenu["Alive.Q"].Cast<CheckBox>().CurrentValue && !Player.Instance.IsRecalling() && QCount < 3 && QCount > 0 && LastCastQ + 3480 < Environment.TickCount && Player.Instance.HasBuff("RivenTriCleaveBuff") && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Player.CastSpell(SpellSlot.Q,
                    Orbwalker.LastTarget != null && Orbwalker.LastAutoAttack - Environment.TickCount < 3000
                        ? Orbwalker.LastTarget.Position
                        : Game.CursorPos);
                return;
            }
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {     
                if (HPpot && Player.Instance.HealthPercent < HPv && _Player.Distance(enemy) < 2000)
                {
                    if (Item.HasItem(Healthpot.Id) && Item.CanUseItem(Healthpot.Id) && !Player.HasBuff("RegenerationPotion"))
                    {
                        Healthpot.Cast();
                    }
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
            if (ComboMenu["FlashBurst"].Cast<KeyBind>().CurrentValue)
            {
                Burst();
                if (TargetSelector.SelectedTarget == null)
                {
                    Orbwalker.OrbwalkTo(Game.CursorPos);
                }
            }
            if (ComboMenu["FlashW"].Cast<KeyBind>().CurrentValue)
            {
                FlashWReal();
                if (TargetSelector.SelectedTarget == null)
                {
                    Orbwalker.OrbwalkTo(Game.CursorPos);
                }
            }
        }


        private static void Burst()
        {

            if (Orbwalker.IsAutoAttacking) return;
            var target = TargetSelector.SelectedTarget;
            if (target == null || !target.IsValidTarget()) return;
            Orbwalker.ForcedTarget = target;
            Orbwalker.OrbwalkTo(target.ServerPosition);
            if (target.IsValidTarget() && !target.IsZombie)
            {
                if (R.IsReady() && R.Name == "RivenFengShuiEngine" && W.IsReady() && E.IsReady() && Q.IsReady() &&
                    _Player.Distance(target.Position) <= 250 + 70 + _Player.AttackRange && QCount == 2)
                {
                    Player.CastSpell(SpellSlot.E, target.Position);
                    Player.CastSpell(SpellSlot.R);
                    Player.CastSpell(SpellSlot.Q, target.Position);
                    if (Item.HasItem(3074) && Item.CanUseItem(3074))
                    {
                        Item.UseItem(3074);
                    }
                    R.Cast(target);
                    Player.CastSpell(SpellSlot.W);
                }
                else if (R.IsReady() && R.Name == "RivenFengShuiEngine" && E.IsReady() && W.IsReady() && Q.IsReady() &&
                         _Player.Distance(target.Position) <= 400 + 70 + _Player.AttackRange)
                {
                    Player.CastSpell(SpellSlot.E, target.Position);
                    Player.CastSpell(SpellSlot.R);
                    Player.CastSpell(SpellSlot.Q, target.Position);
                    if (Item.HasItem(3074) && Item.CanUseItem(3074))
                    {
                        Item.UseItem(3074);
                    }
                    R.Cast(target);
                    Player.CastSpell(SpellSlot.W);
                }
                else if (Flash.IsReady && Q.IsReady() && W.IsReady() && E.IsReady()
                         && R.IsReady() && R.Name == "RivenFengShuiEngine" && (_Player.Distance(target.Position) <= 800) &&
                         ((Item.HasItem(3074) && Item.CanUseItem(3074))))

                {
                    Player.CastSpell(SpellSlot.E, target.Position);
                    Player.CastSpell(SpellSlot.R);
                    Core.DelayAction(FlashQ, 150);
                    Player.CastSpell(SpellSlot.W);
                    if (Item.HasItem(3074) && Item.CanUseItem(3074))
                    {
                        Item.UseItem(3074);
                    }
                    R.Cast(target);
                    Player.CastSpell(SpellSlot.Q, target.Position);

                }
                else if (Flash.IsReady
                         && R.IsReady() && E.IsReady() && W.IsReady() && Q.IsReady() && R.Name == "RivenFengShuiEngine" &&
                         (_Player.Distance(target.Position) <= 800) && Item.HasItem(3074) && Item.CanUseItem(3074))
                {
                    Player.CastSpell(SpellSlot.E, target.Position);
                    Player.CastSpell(SpellSlot.R);
                    Core.DelayAction(FlashW, 150);
                    Player.CastSpell(SpellSlot.W);
                    if (Item.HasItem(3074) && Item.CanUseItem(3074))
                    {
                        Item.UseItem(3074);
                    }
                    R.Cast(target);
                    Player.CastSpell(SpellSlot.Q, target.Position);
                }
            }
        }

        private static void FlashQ()
        {
            var target = TargetSelector.SelectedTarget;
            if (target != null && target.IsValidTarget() && !target.IsZombie)
            {
                Player.CastSpell(SpellSlot.Q, target.Position);
                Core.DelayAction(() => _Player.Spellbook.CastSpell(Flash.Slot, target.Position), 1);
            }
        }

        private static void FlashWReal()
        {
            var target = TargetSelector.SelectedTarget;
            Orbwalker.OrbwalkTo(target.ServerPosition);
            if (target != null && target.Distance(_Player) < 425 + E.Range)
            {
                if (W.IsReady() && E.IsReady() && Flash.IsReady)
                {
                    Player.CastSpell(SpellSlot.E, target.Position);
                    _Player.Spellbook.CastSpell(Flash.Slot, target.Position);
                    Player.CastSpell(SpellSlot.W);
                }
                else
                {
                    return;
                }
            }
        }

        private static void FlashW()
        {
            var target = TargetSelector.SelectedTarget;
            if (target != null && target.IsValidTarget() && !target.IsZombie)
            {
                Player.CastSpell(SpellSlot.W);
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
            if (args.Target is Obj_AI_Turret || args.Target is Obj_Barracks || args.Target is Obj_BarracksDampener ||
                args.Target is Obj_Building)
                if (args.Target.IsValid && args.Target != null && Q.IsReady() && FarmingMenu["QLaneClear"].Cast<CheckBox>().CurrentValue &&
                    Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                    Player.CastSpell(SpellSlot.Q, (Obj_AI_Base)args.Target);
            AIHeroClient client = args.Target as AIHeroClient;
            if (client != null)
            {
                var target = client;
                if (!target.IsValidTarget()) return;
                if (ComboMenu["FlashBurst"].Cast<CheckBox>().CurrentValue)
                {
                    if (Item.HasItem(3074) && Item.CanUseItem(3074))
                    {
                        Item.UseItem(3074);
                        return;
                    }
                    if (R.IsReady() && R.Name == "rivenizunablade")
                    {
                        ForceItem();
                        R.Cast(target);
                    }
                    else if (Q.IsReady())
                    {
                        ForceItem();
                        Player.CastSpell(SpellSlot.Q, target.Position);
                    }
                }
            }
            if (args.SData.Name.ToLower().Contains(Q.Name.ToLower()))
            {
                LastCastQ = Environment.TickCount;
                if (!MiscMenu["Alive.Q"].Cast<CheckBox>().CurrentValue) return;
                Core.DelayAction(() =>
                {
                    if (!Player.Instance.IsRecalling() && QCount <= 2)
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
                    t = DelayMenu["spell1a1b"].Cast<Slider>().CurrentValue;
                    QCount = 1;
                    break;
                case "Spell1b":
                    t = DelayMenu["spell1a1b"].Cast<Slider>().CurrentValue;
                    QCount = 2;
                    break;
                case "Spell1c":
                    t = DelayMenu["spell1c"].Cast<Slider>().CurrentValue;
                    QCount = 0;
                    break;
                case "Spell2":
                    t = DelayMenu["spell2"].Cast<Slider>().CurrentValue;
                    break;
                case "Spell3":
                    break;
                case "Spell4a":
                    t = DelayMenu["spell4a"].Cast<Slider>().CurrentValue;
                    break;
                case "Spell4b":
                    t = DelayMenu["spell4b"].Cast<Slider>().CurrentValue;
                    break;
            }
            if (t != 0 && ((Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None) || ComboMenu["FlashBurst"].Cast<KeyBind>().CurrentValue))
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
                if (Q.IsReady() && Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None || ComboMenu["FlashBurst"].Cast<KeyBind>().CurrentValue && target != null)
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

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
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

        private static readonly float _barLength = 104;
        private static readonly float _xOffset = 2;
        private static readonly float _yOffset = 9;
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (_Player.IsDead)
                return;
            if (!DrawMenu["DrawDamage"].Cast<CheckBox>().CurrentValue) return;
            foreach (var aiHeroClient in EntityManager.Heroes.Enemies)
            {
                if (!aiHeroClient.IsHPBarRendered || !aiHeroClient.VisibleOnScreen) continue;
                var pos = new Vector2(aiHeroClient.HPBarPosition.X + _xOffset, aiHeroClient.HPBarPosition.Y + _yOffset);
                var fullbar = (_barLength) * (aiHeroClient.HealthPercent / 100);
                var damage = (_barLength) *
                                 ((getComboDamage(aiHeroClient) / aiHeroClient.MaxHealth) > 1
                                     ? 1
                                     : (getComboDamage(aiHeroClient) / aiHeroClient.MaxHealth));
                Line.DrawLine(System.Drawing.Color.Gray, 9f, new Vector2(pos.X, pos.Y),
                    new Vector2(pos.X + (damage > fullbar ? fullbar : damage), pos.Y));
                Line.DrawLine(System.Drawing.Color.Black, 9, new Vector2(pos.X + (damage > fullbar ? fullbar : damage) - 2, pos.Y), new Vector2(pos.X + (damage > fullbar ? fullbar : damage) + 2, pos.Y));
            }
        }

        private static float getComboDamage(Obj_AI_Base enemy)
        {
            if (enemy != null)
            {
                float damage = 0;
                float passivenhan;
                if (_Player.Level >= 18)
                {
                    passivenhan = 0.5f;
                }
                else if (_Player.Level >= 15)
                {
                    passivenhan = 0.45f;
                }
                else if (_Player.Level >= 12)
                {
                    passivenhan = 0.4f;
                }
                else if (_Player.Level >= 9)
                {
                    passivenhan = 0.35f;
                }
                else if (_Player.Level >= 6)
                {
                    passivenhan = 0.3f;
                }
                else if (_Player.Level >= 3)
                {
                    passivenhan = 0.25f;
                }
                else
                {
                    passivenhan = 0.2f;
                }
                if (Item.HasItem(3074)) damage = damage + _Player.GetAutoAttackDamage(enemy) * 0.7f;
                if (W.IsReady()) damage = damage + ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W);
                if (Q.IsReady())
                {
                    var qnhan = 4 - QCount;
                    damage = damage + ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q) * qnhan +
                             _Player.GetAutoAttackDamage(enemy) * qnhan * (1 + passivenhan);
                }
                damage = damage + _Player.GetAutoAttackDamage(enemy) * (1 + passivenhan);
                if (R.IsReady())
                {
                    return damage * 1.2f + ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.R);
                }

                return damage;
            }
            return 0;
        }

        public static void ComboAfterAa(Obj_AI_Base target)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

            }         
        }

        public static void HarassAfterAa(Obj_AI_Base target)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

            }
        }

        public static void LastHitAfterAa(Obj_AI_Base target)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

            }
        }

        public static void LaneClearAfterAa(Obj_AI_Base target)
        {
            try {
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

            }
        }

        public static void JungleAfterAa(Obj_AI_Base target)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

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
            try {
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
                    if ((ComboMenu["RCantKill"].Cast<CheckBox>().CurrentValue &&
                        target.Health > Damage.ComboDamage(target, true)
                        && target.Health < Damage.ComboDamage(target)
                        && target.Health > Player.Instance.GetAutoAttackDamage(target, true) * 2) ||
                        (ComboMenu["REnemyCount"].Cast<Slider>().CurrentValue > 0 &&
                        Player.Instance.CountEnemiesInRange(600) >= ComboMenu["REnemyCount"].Cast<Slider>().CurrentValue) || IsRActive)
                    {
                        EnableR = true;
                    }
                    else if((!ComboMenu["RCantKill"].Cast<CheckBox>().CurrentValue && ComboMenu["REnemyCount"].Cast<Slider>().CurrentValue == 0) || ComboMenu["ForcedR"].Cast<KeyBind>().CurrentValue)
                    {
                        EnableR = true;
                    }
                }

                if (ComboMenu["ECombo"].Cast<CheckBox>().CurrentValue && target.Distance(Player.Instance) > W.Range && E.IsReady())
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

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

            try {
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

            }
        }

        private static void JungleClear()
        {
            var minion =
                 EntityManager.MinionsAndMonsters.Monsters.OrderByDescending(a => a.MaxHealth)
                     .FirstOrDefault(a => a.Distance(Player.Instance) < Player.Instance.GetAutoAttackRange(a));

            try {
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

            }
        }
        public static void LaneClear()
        {          
            Orbwalker.ForcedTarget = null;
            try {
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

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
            try {
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {

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
                Drawing.DrawCircle(_Player.Position, Q.Range + E.Range, Color.Red);
            }
            if (DrawMenu["drawFBurst"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawCircle(_Player.Position, 425 + E.Range, Color.Green);
            }
            if (DrawMenu["drawStatus"].Cast<CheckBox>().CurrentValue)
            {                
                var pos = Drawing.WorldToScreen(Player.Instance.Position);
                Drawing.DrawText((int)pos.X - 45, (int)pos.Y + 40, Color.DarkGray, "Forced R: " + IsRActive);
            }
        }

        //
        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (sender.IsMe || sender.IsAlly || args.SData.IsAutoAttack()) return;
                var articunoPerfectCheck = _Player.Position.PointOnLineSegment(args.Start,
                    args.Start.Extend(args.End, args.SData.CastRangeDisplayOverride).To3D());
                if (DB.Contains(args.SData.Name) &&
                    E.IsReady() &&
                    (articunoPerfectCheck || (args.Target != null && args.Target.IsMe)))
                {
                    E.Cast(Game.CursorPos);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Got Milk: " + ex);
            }
        }
    }
}