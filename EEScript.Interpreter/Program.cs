using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using CLAP;
using BotBits;
using BotBits.Events;
using PlayerIOClient;
using BotBitsExt.Physics;
using BotBitsExt.Movement;
using BotBitsExt.Movement.Events;

namespace EEScript.Interpreter
{
    using static Page;
    using Enums;

    class Program
    {
        static void Main(string[] args)
        {
            Parser.RunConsole<EEScriptInterpreter>(args);

            Thread.Sleep(-1);
        }
    }

    // A miniature version of RabbitIO (https://github.com/Decagon/Rabbit).
    public static class Authentication
    {
        public enum AuthenticationType { Invalid, Unknown, Facebook, Kongregate, ArmorGames, Simple, Public, UserId = Simple }

        /// <summary>Connects to the PlayerIO service using the provided credentials.</summary>
        /// <param name="user">The user id, token or email address.</param>
        /// <param name="auth">The password or temporary key.</param>
        public static Client LogOn(string gameid, string user = "", string auth = "", AuthenticationType type = AuthenticationType.Unknown)
        {
            user = Regex.Replace(user, @"\s+", string.Empty);
            gameid = Regex.Replace(gameid, @"\s+", string.Empty);

            if (type == AuthenticationType.Unknown)
                type = GetAuthType(user, auth);

            return Authenticate(gameid, user, auth, type);
        }

        private static AuthenticationType GetAuthType(string user, string auth)
        {
            if (string.IsNullOrEmpty(auth))
                throw new ArgumentNullException("auth");

            if (string.IsNullOrEmpty(user)) {
                if (Regex.IsMatch(auth, @"[0-9a-z]$", RegexOptions.IgnoreCase) && auth.Length > 90)
                    return AuthenticationType.Facebook;
                return AuthenticationType.Invalid;
            }

            if (Regex.IsMatch(auth, @"\A\b[0-9a-fA-F]+\b\Z")) {
                if (user.Length == 32 && auth.Length == 32)
                    return AuthenticationType.ArmorGames;
                if (Regex.IsMatch(user, @"^\d+$") && auth.Length == 64)
                    return AuthenticationType.Kongregate;
            }

            if (Regex.IsMatch(user, @"\b+[a-zA-Z0-9\.\-_]+@[a-zA-Z0-9\.\-]+\.[a-zA-Z0-9\.\-]+\b"))
                return AuthenticationType.Simple;

            return AuthenticationType.UserId;
        }

        private static Client Authenticate(string gameid, string user, string auth, AuthenticationType type = AuthenticationType.Invalid) =>
               type == AuthenticationType.Facebook ? PlayerIO.QuickConnect.FacebookOAuthConnect(gameid, auth, null, null) :
               type == AuthenticationType.Simple ? PlayerIO.QuickConnect.SimpleConnect(gameid, user, auth, null) :
               type == AuthenticationType.Kongregate ? PlayerIO.QuickConnect.KongregateConnect(gameid, user, auth, null) :
               type == AuthenticationType.ArmorGames ? PlayerIO.Authenticate(gameid, "public", new Dictionary<string, string> { { "userId", user }, { "authToken", auth } }, null) :
               type == AuthenticationType.Public ? PlayerIO.Connect(gameid, "public", user, auth, null) : null;
    }

    public class EEScriptInterpreter
    {
        public static BotBitsClient Client { get; set; }
        public static EEScriptEngine Engine { get; set; }

        public static Page Page { get; set; }
        public static Random Random { get; set; }

        public static string GameId { get; set; } = "everybody-edits-su9rn58o40itdbnw69plyw";
        public static string UsernameOrEmail { get; set; }
        public static string AuthenticationKey { get; set; }
        public static string WorldId { get; set; }
        public static string PageSource { get; set; }

        [Verb(Aliases = "start")]
        public static void Execute(
            [Aliases("u,email")]string usernameOrEmail,
            [Aliases("a,auth,p,pass,password")]string authenticationKey,
            [Aliases("t,w,id,world")]string worldId,
            [Aliases("src,page")]string pageSource)
        {
            Random = new Random();

            Client = new BotBitsClient();
            Engine = new EEScriptEngine();

            UsernameOrEmail = usernameOrEmail;
            AuthenticationKey = authenticationKey;
            WorldId = worldId;
            PageSource = pageSource;

            // setup page
            Page = Engine.LoadFromString(pageSource);

            // setup triggerss
            SetupTriggers();

            // setup variable handler
            Page.VariableHandler += (Trigger trigger, string key) => {
                var player = (Player)trigger.TriggeringEntity;

                return player.Get<object>(key);
            };

            EventLoader.Of(Client).LoadStatic<EEScriptInterpreter>();

            // load extensions
            MovementExtension.LoadInto(Client);
            PhysicsExtension.LoadInto(Client);

            // start interpreter
            Login.Of(Client).WithClient(Authentication.LogOn(GameId, UsernameOrEmail, AuthenticationKey)).CreateJoinRoom(WorldId);

            Thread.Sleep(Timeout.Infinite);
        }

        #region Events
        [EventListener]
        public static void On(JoinCompleteEvent e)
        {
            // When everything has started up,
            Page.Execute(null, e, 1);
        }

        [EventListener]
        public static void On(JoinEvent e)
        {
            // Whenever someone arrives in the world,
            Page.Execute(e.Player, e, 64);
        }

        [EventListener]
        public static void On(LeaveEvent e)
        {
            // Whenever someone leaves the world,
            Page.Execute(e.Player, e, 65);
        }

        [EventListener]
        public static void On(MoveEvent e)
        {
            // Whenever someone jumps,
            if (e.SpaceJustDown)
                Page.Execute(e.Player, e, 81);
        }

        [EventListener]
        public static void On(BlockChangeEvent e)
        {
            // Whenever someone moves,
            Page.Execute(e.Player, e, 66);

            // Whenever someone moves into block #,
            Page.Execute(e.Player, e, 71);

            // Whenever someone moves into position (#,#),
            Page.Execute(e.Player, e, 73);
        }

        [EventListener]
        public static void On(MoveInDirectionEvent e)
        {
            switch (e.Direction) {
                case Direction.Up:
                    // Whenever someone moves up,
                    Page.Execute(e.Player, e, 69);
                    break;
                case Direction.Down:
                    // Whenever someone moves down,
                    Page.Execute(e.Player, e, 70);
                    break;
                case Direction.Left:
                    // Whenever someone moves left,
                    Page.Execute(e.Player, e, 67);
                    break;
                case Direction.Right:
                    // Whenever someone moves right,
                    Page.Execute(e.Player, e, 68);
                    break;
                case Direction.None:
                    // Whenever someone stops moving,
                    Page.Execute(e.Player, e, 78);
                    break;
            }
        }

        [EventListener]
        public static void On(ChatEvent e)
        {
            // Whenever someone says anything,
            Page.Execute(e.Player, e, 85);

            // Whenever someone says {...},
            Page.Execute(e.Player, e, 86);

            // Whenever someone says something with {...} in it,
            Page.Execute(e.Player, e, 87);
        }

        [EventListener]
        public static void On(PrivateMessageEvent e)
        {
            // Whenever someone private messages {...},
            Page.Execute(Players.Of(Client).FromUsername(e.Username), e, 88);

            // Whenever someone private messages something with {...} in it,
            Page.Execute(Players.Of(Client).FromUsername(e.Username), e, 88);
        }

        [EventListener]
        public static void On(ForegroundPlaceEvent e)
        {
            // Whenever someone places a block anywhere,
            Page.Execute(e.New.Placer, e, 90);

            // Whenever someone places a foreground block # at (#,#),
            Page.Execute(e.New.Placer, e, 91);

            // Whenever someone places a foreground block at (#,#),
            Page.Execute(e.New.Placer, e, 92);

            // Whenever someone places a foreground block #,
            Page.Execute(e.New.Placer, e, 95);

            // Whenever someone removes a foreground block #,
            if (!e.Old.Block.Equals(e.New.Block) && e.New.Block.Id == Foreground.Empty)
                Page.Execute(e.New.Placer, e, 96);
        }

        [EventListener]
        public static void On(BackgroundPlaceEvent e)
        {
            // Whenever someone places a block anywhere,
            Page.Execute(e.New.Placer, e, 90);

            // Whenever someone places a background block # at (#,#),
            Page.Execute(e.New.Placer, e, 93);

            // Whenever someone places a background block at (#,#),
            Page.Execute(e.New.Placer, e, 94);

            // Whenever someone places a background block #,
            Page.Execute(e.New.Placer, e, 97);

            // Whenever someone removes a background block #,
            if (!e.Old.Block.Equals(e.New.Block) && e.New.Block.Id == Background.Empty)
                Page.Execute(e.New.Placer, e, 98);
        }

        [EventListener]
        public static void On(KillEvent e)
        {
            // Whenever someone dies,
            Page.Execute(e.Player, e, 100);
        }

        [EventListener]
        public static void On(TeamEvent e)
        {
            // Whenever someone changes teams,
            Page.Execute(e.Player, e, 101);
        }

        [EventListener]
        public static void On(GoldCoinEvent e)
        {
            // Whenever someone collects a gold coin,
            Page.Execute(e.Player, e, 102);
        }

        [EventListener]
        public static void On(BlueCoinEvent e)
        {
            // Whenever someone collects a blue coin,
            Page.Execute(e.Player, e, 103);
        }

        [EventListener]
        public static void On(CrownEvent e)
        {
            // Whenever someone touches a golden crown,
            Page.Execute(e.Player, e, 104);
        }

        [EventListener]
        public static void On(SilverCrownEvent e)
        {
            // Whenever someone touches a silver crown,
            Page.Execute(e.Player, e, 105);
        }

        [EventListener]
        public static void On(HideKeyEvent e)
        {
            // Whenever someone touches a key,
            Page.Execute(e.Player, e, 110);
        }

        [EventListener]
        public static void On(SmileyEvent e)
        {
            switch (e.Smiley) {
                // Whenever someone touches a cake,
                case Smiley.BirthdayGrin:
                case Smiley.BirthdayHappy:
                case Smiley.BirthdaySmile:
                case Smiley.BirthdayTongue:
                    Page.Execute(e.Player, e, 111);
                    break;
            }

            // Whenever someone changes their smiley,
            Page.Execute(e.Player, e, 124);

            // Whenever someone changes their smiley to #,
            Page.Execute(e.Player, e, 125);
        }

        [EventListener]
        public static void On(PurpleSwitchEvent e)
        {
            // Whenever someone toggles a purple switch with the Id #,
            if (e.Enabled) {
                Page.Execute(e.Player, e, 164);
            }

            // Whenever someone untoggles a purple switch with the Id #,
            if (!e.Enabled) {
                Page.Execute(e.Player, e, 164);
            }
        }

        [EventListener]
        public static void On(OrangeSwitchEvent e)
        {
            // Whenever someone toggles an orange switch with the Id #,
            if (e.Enabled) {
                Page.Execute(null, e, 166);
            }

            // Whenever someone untoggles an orange switch with the Id #,
            if (!e.Enabled) {
                Page.Execute(null, e, 167);
            }
        }

        [EventListener]
        public static void On(MetaChangedEvent e)
        {
            // Whenever the world name is changed to anything,
            Page.Execute(null, e, 200);

            // Whenever the world name is changed to {...},"
            Page.Execute(null, e, 201);

            // WWhenever the world name is changed to something with {...} in it,
            Page.Execute(null, e, 202);
        }

        #endregion

        #region Triggers
        static void SetupTriggers()
        {
            #region Cause
            // When everything is starting up,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 0), new TriggerHandler((trigger, player, args) => {
                return true;
            }));

            // Whenever someone moves into block #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 71), new TriggerHandler((trigger, player, args) => {
                var blockChangeEvent = (BlockChangeEvent)args;

                if (blockChangeEvent.BlockId == trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone moves into position (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 73), new TriggerHandler((trigger, player, args) => {
                var blockChangeEvent = (BlockChangeEvent)args;

                if (blockChangeEvent.BlockX == trigger.GetInt(0) && blockChangeEvent.BlockY == trigger.GetInt(1)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone says {...},
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 86), new TriggerHandler((trigger, player, args) => {
                var chatEvent = (ChatEvent)args;

                if (chatEvent.Text.ToLower() == trigger.GetString(0).ToLower()) {
                    return true;
                }

                return false;
            }));

            // Whenever someone says something with {...} in it,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 87), new TriggerHandler((trigger, player, args) => {
                var chatEvent = (ChatEvent)args;

                if (chatEvent.Text.ToLower().Contains(trigger.GetString(0).ToLower())) {
                    return true;
                }

                return false;
            }));

            // Whenever someone private messages {...},
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 88), new TriggerHandler((trigger, player, args) => {
                var privateMessageEvent = (PrivateMessageEvent)args;

                if (privateMessageEvent.Message.ToLower() == trigger.GetString(0).ToLower()) {
                    return true;
                }

                return false;
            }));

            // Whenever someone private messages something with {...} in it,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 89), new TriggerHandler((trigger, player, args) => {
                var privateMessageEvent = (PrivateMessageEvent)args;

                if (privateMessageEvent.Message.ToLower().Contains(trigger.GetString(0).ToLower())) {
                    return true;
                }

                return false;
            }));

            // Whenever someone places a foreground block # at (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 91), new TriggerHandler((trigger, player, args) => {
                var blockEvent = (ForegroundPlaceEvent)args;

                if (blockEvent.New.Block.Id == (Foreground.Id)trigger.GetInt(0)) {
                    if (blockEvent.X == trigger.GetInt(1) && blockEvent.Y == trigger.GetInt(2)) {
                        return true;
                    }
                }

                return false;
            }));

            // Whenever someone places a foreground block at (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 92), new TriggerHandler((trigger, player, args) => {
                var blockEvent = (ForegroundPlaceEvent)args;

                if (blockEvent.X == trigger.GetInt(1) && blockEvent.Y == trigger.GetInt(2)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone places a background block # at (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 93), new TriggerHandler((trigger, player, args) => {
                var blockEvent = (BackgroundPlaceEvent)args;

                if (blockEvent.New.Block.Id == (Background.Id)trigger.GetInt(0)) {
                    if (blockEvent.X == trigger.GetInt(1) && blockEvent.Y == trigger.GetInt(2)) {
                        return true;
                    }
                }

                return false;
            }));

            // Whenever someone places a background block at (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 94), new TriggerHandler((trigger, player, args) => {
                var blockEvent = (BackgroundPlaceEvent)args;

                if (blockEvent.X == trigger.GetInt(1) && blockEvent.Y == trigger.GetInt(2)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone places a foreground block #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 95), new TriggerHandler((trigger, player, args) => {
                var blockEvent = (ForegroundPlaceEvent)args;

                if (blockEvent.New.Block.Id == (Foreground.Id)trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone removes a foreground block #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 96), new TriggerHandler((trigger, player, args) => {
                var blockEvent = (ForegroundPlaceEvent)args;

                if (blockEvent.Old.Block.Id == (Foreground.Id)trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone places a background block #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 97), new TriggerHandler((trigger, player, args) => {
                var blockEvent = (BackgroundPlaceEvent)args;

                if (blockEvent.New.Block.Id == (Background.Id)trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone removes a background block #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 98), new TriggerHandler((trigger, player, args) => {
                var blockEvent = (BackgroundPlaceEvent)args;

                if (blockEvent.Old.Block.Id == (Background.Id)trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone changes their smiley to #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 125), new TriggerHandler((trigger, player, args) => {
                var smileyEvent = (SmileyEvent)args;

                if (smileyEvent.Smiley == (Smiley)trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone toggles a purple switch with the Id #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 164), new TriggerHandler((trigger, player, args) => {
                var purpleSwitchEvent = (PurpleSwitchEvent)args;

                if (purpleSwitchEvent.SwitchId == trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone untoggles a purple switch with the Id #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 165), new TriggerHandler((trigger, player, args) => {
                var purpleSwitchEvent = (PurpleSwitchEvent)args;

                if (purpleSwitchEvent.SwitchId == trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone toggles an orange switch with the Id #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 166), new TriggerHandler((trigger, player, args) => {
                var orangeSwitchEvent = (OrangeSwitchEvent)args;

                if (orangeSwitchEvent.SwitchId == trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever someone untoggles an orange switch with the Id #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 167), new TriggerHandler((trigger, player, args) => {
                var orangeSwitchEvent = (OrangeSwitchEvent)args;

                if (orangeSwitchEvent.SwitchId == trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever the world name is changed to {...},
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 201), new TriggerHandler((trigger, player, args) => {
                var metaChangedEvent = (MetaChangedEvent)args;

                if (metaChangedEvent.WorldName == trigger.GetString(0)) {
                    return true;
                }

                return false;
            }));

            // Whenever the world name is changed to something with {...} in it,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 202), new TriggerHandler((trigger, player, args) => {
                var metaChangedEvent = (MetaChangedEvent)args;

                if (metaChangedEvent.WorldName.ToLower().Contains(trigger.GetString(0).ToLower())) {
                    return true;
                }

                return false;
            }));

            #endregion

            #region Condition

            // and they are currently on foreground block #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 67), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;

                if (Blocks.Of(Client).At(_player.GetPhysicsPlayer().BlockX, _player.GetPhysicsPlayer().BlockY).Foreground.Block.Id == (Foreground.Id)trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // and they are not currently on foreground block #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 68), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;

                if (Blocks.Of(Client).At(_player.GetPhysicsPlayer().BlockX, _player.GetPhysicsPlayer().BlockY).Foreground.Block.Id != (Foreground.Id)trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // and they are currently on background block #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 69), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;

                if (Blocks.Of(Client).At(_player.GetPhysicsPlayer().BlockX, _player.GetPhysicsPlayer().BlockY).Background.Block.Id == (Background.Id)trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // and they are not currently on background block #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 70), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;

                if (Blocks.Of(Client).At(_player.GetPhysicsPlayer().BlockX, _player.GetPhysicsPlayer().BlockY).Background.Block.Id != (Background.Id)trigger.GetInt(0)) {
                    return true;
                }

                return false;
            }));

            // and they are currently at position (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 71), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;

                if (_player.GetPhysicsPlayer().BlockX == trigger.GetInt(0) && _player.GetPhysicsPlayer().BlockY == trigger.GetInt(1)) {
                    return true;
                }

                return false;
            }));

            // and they are not currently at position (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 72), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;

                if (_player.GetPhysicsPlayer().BlockX != trigger.GetInt(0) && _player.GetPhysicsPlayer().BlockY != trigger.GetInt(1)) {
                    return true;
                }

                return false;
            }));

            // and they can see position (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 85), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;

                for (var x = _player.GetPhysicsPlayer().BlockX - 20; x < _player.GetPhysicsPlayer().BlockX + 20; x++) {
                    for (var y = _player.GetPhysicsPlayer().BlockY - 15; y < _player.GetPhysicsPlayer().BlockY + 15; y++) {

                        if (x == trigger.GetInt(0) && y == trigger.GetInt(1)) {
                            return true;
                        }

                    }
                }

                return false;
            }));


            // and they are the world owner,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 128), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).Owner;
            }));

            // and their name is {...},
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 129), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).ChatName.ToLower() == trigger.GetString(0).ToLower();
            }));

            // and their name is not {...},
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 130), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).ChatName.ToLower() != trigger.GetString(0).ToLower();
            }));

            // and their unique user id is {...}
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 131), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).ConnectUserId == trigger.GetString(0);
            }));

            // and their unique user id is not {...}
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 132), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).ConnectUserId != trigger.GetString(0);
            }));

            // and their team is #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 135), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).Team == (Team)trigger.GetInt(0);
            }));

            // and their team is not #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 136), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).Team != (Team)trigger.GetInt(0);
            }));

            // and they have # gold coins,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 450), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).GoldCoins == trigger.GetInt(0);
            }));

            // and they have # blue coins,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 451), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).BlueCoins == trigger.GetInt(0);
            }));

            // and they have more than # gold coins,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 452), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).GoldCoins > trigger.GetInt(0);
            }));

            // and they have less than # gold coins,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 453), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).GoldCoins < trigger.GetInt(0);
            }));

            // and they have more than # blue coins,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 454), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).BlueCoins > trigger.GetInt(0);
            }));

            // and they have less than # blue coins,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 455), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).BlueCoins < trigger.GetInt(0);
            }));

            // and they have god mode enabled,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 462), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).GodMode;
            }));

            // and they have god mode disabled,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 463), new TriggerHandler((trigger, player, args) => {
                return !((Player)player).GodMode;
            }));

            // and they have mod mode enabled,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 464), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).ModMode;
            }));

            // and they have mod mode disabled,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 465), new TriggerHandler((trigger, player, args) => {
                return !((Player)player).ModMode;
            }));

            // and they have admin mode enabled,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 466), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).AdminMode;
            }));

            // and they have admin mode disabled,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 467), new TriggerHandler((trigger, player, args) => {
                return !((Player)player).AdminMode;
            }));

            // and they have got edit access (or is the world owner),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 474), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).HasEditRights;
            }));

            // and they haven't got edit access (and is not the world owner),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 475), new TriggerHandler((trigger, player, args) => {
                return !((Player)player).HasEditRights;
            }));

            // and they have got gold membership,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 476), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).ClubMember;
            }));

            // and they haven't got gold membership,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 477), new TriggerHandler((trigger, player, args) => {
                return !((Player)player).ClubMember;
            }));

            // and their team is set to #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 500), new TriggerHandler((trigger, player, args) => {
                return ((Player)player).Team == (Team)trigger.GetInt(0);
            }));

            // and the foreground block at (#,#) is #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 600), new TriggerHandler((trigger, player, args) => {
                if (Blocks.Of(Client).At(trigger.GetInt(0), trigger.GetInt(1)).Foreground.Block.Id == (Foreground.Id)trigger.GetInt(2)) {
                    return true;
                }

                return false;
            }));

            // and the background block at (#,#) is #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 601), new TriggerHandler((trigger, player, args) => {
                if (Blocks.Of(Client).At(trigger.GetInt(0), trigger.GetInt(1)).Background.Block.Id == (Background.Id)trigger.GetInt(2)) {
                    return true;
                }

                return false;
            }));

            // and the foreground block at (#,#) is not #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 602), new TriggerHandler((trigger, player, args) => {
                if (Blocks.Of(Client).At(trigger.GetInt(0), trigger.GetInt(1)).Foreground.Block.Id != (Foreground.Id)trigger.GetInt(2)) {
                    return true;
                }

                return false;
            }));

            // and the background block at (#,#) is not #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 603), new TriggerHandler((trigger, player, args) => {
                if (Blocks.Of(Client).At(trigger.GetInt(0), trigger.GetInt(1)).Background.Block.Id != (Background.Id)trigger.GetInt(2)) {
                    return true;
                }

                return false;
            }));

            // and the foreground block at (#,#) is the same block at (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 604), new TriggerHandler((trigger, player, args) => {
                var first = Blocks.Of(Client).At(trigger.GetInt(0), trigger.GetInt(1)).Foreground.Block;
                var second = Blocks.Of(Client).At(trigger.GetInt(2), trigger.GetInt(3)).Foreground.Block;

                if (first.Equals(second)) {
                    return true;
                }

                return false;
            }));

            // and the background block at (#,#) is the same block at (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 605), new TriggerHandler((trigger, player, args) => {
                var first = Blocks.Of(Client).At(trigger.GetInt(0), trigger.GetInt(1)).Background.Block;
                var second = Blocks.Of(Client).At(trigger.GetInt(2), trigger.GetInt(3)).Background.Block;

                if (first.Equals(second)) {
                    return true;
                }

                return false;
            }));

            // and the foreground block at (#,#) is NOT the same block at (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 606), new TriggerHandler((trigger, player, args) => {
                var first = Blocks.Of(Client).At(trigger.GetInt(0), trigger.GetInt(1)).Foreground.Block;
                var second = Blocks.Of(Client).At(trigger.GetInt(2), trigger.GetInt(3)).Foreground.Block;

                if (!first.Equals(second)) {
                    return true;
                }

                return false;
            }));

            // and the background block at (#,#) is NOT the same block at (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 607), new TriggerHandler((trigger, player, args) => {
                var first = Blocks.Of(Client).At(trigger.GetInt(0), trigger.GetInt(1)).Background.Block;
                var second = Blocks.Of(Client).At(trigger.GetInt(2), trigger.GetInt(3)).Background.Block;

                if (!first.Equals(second)) {
                    return true;
                }

                return false;
            }));

            // and the player named {...} is in the world right now,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 632), new TriggerHandler((trigger, player, args) => {
                if (Players.Of(Client).GetPlayers().Any(p => p.Username.ToLower() == trigger.GetString(0).ToLower())) {
                    return true;
                }

                return false;
            }));

            // and the player with the unique user id {...} is in the world right now,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 633), new TriggerHandler((trigger, player, args) => {
                if (Players.Of(Client).GetPlayers().Any(p => p.ConnectUserId == trigger.GetString(0))) {
                    return true;
                }

                return false;
            }));

            // and the player named {...} is not in the world right now,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 634), new TriggerHandler((trigger, player, args) => {
                if (!Players.Of(Client).GetPlayers().Any(p => p.Username.ToLower() == trigger.GetString(0).ToLower())) {
                    return true;
                }

                return false;
            }));

            // and the player with the unique user id {...} is not in the world right now,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 635), new TriggerHandler((trigger, player, args) => {
                if (!Players.Of(Client).GetPlayers().Any(p => p.ConnectUserId == trigger.GetString(0))) {
                    return true;
                }

                return false;
            }));

            // and there's someone at (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 700), new TriggerHandler((trigger, player, args) => {
                if (Players.Of(Client).GetPlayers().Any(p => p.GetPhysicsPlayer().BlockX == trigger.GetInt(0) && p.GetPhysicsPlayer().BlockY == trigger.GetInt(1))) {
                    return true;
                }

                return false;
            }));

            // and there's nobody at (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 701), new TriggerHandler((trigger, player, args) => {
                if (!Players.Of(Client).GetPlayers().Any(p => p.GetPhysicsPlayer().BlockX == trigger.GetInt(0) && p.GetPhysicsPlayer().BlockY == trigger.GetInt(1))) {
                    return true;
                }

                return false;
            }));

            // and the triggering player's variable % is equal to #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 2000), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;

                if (_player.Get<object>(trigger.GetVariableName(0)) == trigger.Get(1)) {
                    return true;
                }

                return false;
            }));

            // and the global variable ~ is equal to #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 2001), new TriggerHandler((trigger, player, args) => {
                var _trigger = Page.Variables.FirstOrDefault(v => v.Key == trigger.GetVariableName(0));

                if (_trigger == null)
                    return false;

                if (_trigger.Value == trigger.Get(1)) {
                    return true;
                }

                return false;
            }));

            // and the triggering player's variable % is more than #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 2002), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;

                if (_player.Get<int>(trigger.GetVariableName(0)) > trigger.GetInt(1)) {
                    return true;
                }

                return false;
            }));

            // and the global variable ~ is more than #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 2003), new TriggerHandler((trigger, player, args) => {
                var _trigger = Page.Variables.FirstOrDefault(v => v.Key == trigger.GetVariableName(0));

                if (_trigger == null)
                    return false;

                if (_trigger.Value is int) {
                    if ((int)_trigger.Value > trigger.GetInt(1)) {
                        return true;
                    }
                }

                return false;
            }));

            // and the triggering player's variable % is less than #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 2004), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;

                if (_player.Get<int>(trigger.GetVariableName(0)) < trigger.GetInt(1)) {
                    return true;
                }

                return false;
            }));

            // and the global variable ~ is less than #,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 2005), new TriggerHandler((trigger, player, args) => {
                var _trigger = Page.Variables.FirstOrDefault(v => v.Key == trigger.GetVariableName(0));

                if (_trigger == null)
                    return false;

                if (_trigger.Value is int) {
                    if ((int)_trigger.Value < trigger.GetInt(1)) {
                        return true;
                    }
                }

                return false;
            }));

            #endregion

            #region Area
            // everywhere in the entire world,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 64), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                for (var x = 0; x < Blocks.Of(Client).Width; x++)
                    for (var y = 0; y < Blocks.Of(Client).Height; y++)
                        trigger.Area.Points.Add(new Point(x, y));

                return true;
            }));

            // at position (#,#) in the world,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 65), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                trigger.Area.Points = new List<Point>() {
                    new Point( trigger.GetInt(0), trigger.GetInt(1))
                };

                return true;
            }));

            // where the triggering player is at,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 128), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                var _player = (Player)player;

                trigger.Area.Points = new List<Point>() {
                    new Point(_player.GetPhysicsPlayer().BlockX, _player.GetPhysicsPlayer().BlockY)
                };

                return true;
            }));

            // everyplace the triggering player can see,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 149), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                var _player = (Player)player;

                for (var x = _player.GetPhysicsPlayer().BlockX - 20; x < _player.GetPhysicsPlayer().BlockX + 20; x++) {
                    for (var y = _player.GetPhysicsPlayer().BlockY - 15; y < _player.GetPhysicsPlayer().BlockY + 15; y++) {
                        trigger.Area.Points.Add(new Point(x, y));
                    }
                }

                return true;
            }));

            // everyplace that can be seen from (#,#)
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 150), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                for (var x = trigger.GetInt(0) - 20; x < trigger.GetInt(0) + 20; x++) {
                    for (var y = trigger.GetInt(1) - 15; y < trigger.GetInt(1) + 15; y++) {
                        trigger.Area.Points.Add(new Point(x, y));
                    }
                }

                return true;
            }));

            // at # block(s) just to the left of the triggering player,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 151), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                var _player = (Player)player;

                trigger.Area.Points.Add(new Point(_player.GetPhysicsPlayer().BlockX - trigger.GetInt(0), _player.GetPhysicsPlayer().BlockY));

                return true;
            }));

            // at # block(s) just to the right of the triggering player,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 152), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                var _player = (Player)player;

                trigger.Area.Points.Add(new Point(_player.GetPhysicsPlayer().BlockX + trigger.GetInt(0), _player.GetPhysicsPlayer().BlockY));

                return true;
            }));

            // at # block(s) just above the triggering player,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 153), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                var _player = (Player)player;

                trigger.Area.Points.Add(new Point(_player.GetPhysicsPlayer().BlockX, _player.GetPhysicsPlayer().BlockY - trigger.GetInt(0)));

                return true;
            }));

            // at # block(s) just below the triggering player,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 154), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                var _player = (Player)player;

                trigger.Area.Points.Add(new Point(_player.GetPhysicsPlayer().BlockX, _player.GetPhysicsPlayer().BlockY + trigger.GetInt(0)));

                return true;
            }));

            // at a random spot somewhere in the world,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 800), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                var positionX = Random.Next(0, Blocks.Of(Client).Width);
                var positionY = Random.Next(0, Blocks.Of(Client).Height);

                trigger.Area.Points.Add(new Point(positionX, positionY));

                return true;
            }));

            // at a random spot that is somewhere onscreen for the triggering player,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 801), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                var _player = (Player)player;

                var positionX = Random.Next(_player.GetPhysicsPlayer().BlockX - 20, _player.GetPhysicsPlayer().BlockX + 20);
                var positionY = Random.Next(_player.GetPhysicsPlayer().BlockY - 15, _player.GetPhysicsPlayer().BlockY + 15);

                trigger.Area.Points.Add(new Point(positionX, positionY));

                return true;
            }));

            // everywhere within the rectangle (TL/BR) (#,#) - (#,#),
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 802), new TriggerHandler((trigger, player, args) => {
                trigger.Area = new Area();

                var rectangle = new Rectangle(new BotBits.Point(trigger.GetInt(0), trigger.GetInt(1)), new BotBits.Point(trigger.GetInt(2), trigger.GetInt(3)));

                for (var i = rectangle.Left; i < rectangle.Right; i++)
                    for (var j = rectangle.Top; j < rectangle.Bottom; j++)
                        trigger.Area.Points.Add(new Point(i, j));

                return true;
            }));
            #endregion

            #region Filter
            // only where the triggering player is currently at,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 64), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;
                var area = trigger.Areas.LastOrDefault().Area;

                if (area == null)
                    return false;

                area.Points = area.Points.Where(x => x.X == _player.GetPhysicsPlayer().X && x.Y == _player.GetPhysicsPlayer().Y).ToList();
                return true;
            }));

            // only where there is a foreground block present,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 65), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;
                var area = trigger.Areas.LastOrDefault().Area;

                if (area == null)
                    return false;

                area.Points = area.Points.Where(b => Blocks.Of(Client).Foreground.Any(x => x.Location.X == b.X && x.Location.Y == b.Y)).ToList();
                return true;
            }));

            // only where there is a background block present,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 66), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;
                var area = trigger.Areas.LastOrDefault().Area;

                if (area == null)
                    return false;

                area.Points = area.Points.Where(b => Blocks.Of(Client).Background.Any(x => x.Location.X == b.X && x.Location.Y == b.Y)).ToList();
                return true;
            }));

            // only where there is a foreground block # present,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 67), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;
                var area = trigger.Areas.LastOrDefault().Area;

                if (area == null)
                    return false;

                area.Points = area.Points.Where(b => Blocks.Of(Client).Foreground.Any(x => x.Location.X == b.X && x.Location.Y == b.Y && x.Data.Block.Id == (Foreground.Id)trigger.GetInt(0))).ToList();
                return true;
            }));

            // only where there is a background block # present,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 68), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;
                var area = trigger.Areas.LastOrDefault().Area;

                if (area == null)
                    return false;

                area.Points = area.Points.Where(b => Blocks.Of(Client).Background.Any(x => x.Location.X == b.X && x.Location.Y == b.Y && x.Data.Block.Id == (Background.Id)trigger.GetInt(0))).ToList();
                return true;
            }));

            // only where there is a block present,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 69), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;
                var area = trigger.Areas.LastOrDefault().Area;

                if (area == null)
                    return false;

                area.Points = area.Points.Where(b => Blocks.Of(Client).Foreground.Any(x => x.Location.X == b.X && x.Location.Y == b.Y) || 
                                                     Blocks.Of(Client).Background.Any(x => x.Location.X == b.X && x.Location.Y == b.Y)).ToList();
                return true;
            }));

            // only where there are no blocks present,
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 70), new TriggerHandler((trigger, player, args) => {
                var _player = (Player)player;
                var area = trigger.Areas.LastOrDefault().Area;

                if (area == null)
                    return false;

                area.Points = area.Points.Where(b => !Blocks.Of(Client).Foreground.Any(x => x.Location.X == b.X && x.Location.Y == b.Y) &&
                                                     !Blocks.Of(Client).Background.Any(x => x.Location.X == b.X && x.Location.Y == b.Y)).ToList();
                return true;
            }));

            #endregion

            #region Effect
            // move the triggering player to (#,#).
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 64), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Teleport(trigger.GetInt(0), trigger.GetInt(1));

                return true;
            }));

            // kick the triggering player with the message {...}.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 100), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Kick(trigger.GetString(0));

                return true;
            }));

            // kill the triggering player.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 102), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Kill();

                return true;
            }));

            // give the triggering player edit privileges.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 104), new TriggerHandler((trigger, player, args) => {
                ((Player)player).GiveEdit();

                return true;
            }));

            // remove edit privileges from the triggering player.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 106), new TriggerHandler((trigger, player, args) => {
                ((Player)player).RemoveEdit();

                return true;
            }));

            // give god privileges to the triggering player.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 108), new TriggerHandler((trigger, player, args) => {
                ((Player)player).GiveGod();

                return true;
            }));

            // remove god privileges from the triggering player.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 110), new TriggerHandler((trigger, player, args) => {
                ((Player)player).RemoveGod();

                return true;
            }));

            // give the triggering player a crown.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 132), new TriggerHandler((trigger, player, args) => {
                ((Player)player).GiveCrown();

                return true;
            }));

            // say {...} in the world.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 164), new TriggerHandler((trigger, player, args) => {
                Chat.Of(Client).Say(trigger.GetString(0));

                return true;
            }));

            // enable the world lobby preview.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 166), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetLobbyPreviewEnabled(true);

                return true;
            }));

            // disable the world lobby preview.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 167), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetLobbyPreviewEnabled(false);

                return true;
            }));

            // enable the world visibility.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 168), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetRoomVisible(true);

                return true;
            }));

            // disable the world visibility.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 169), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetRoomVisible(false);

                return true;
            }));

            // enable the world visibility in the lobby.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 170), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetHideLobby(false);

                return true;
            }));

            // disable the world visibility in the lobby.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 171), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetHideLobby(true);

                return true;
            }));

            // change the world name to {...}.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 172), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetName(trigger.GetString(0));

                return true;
            }));

            // change the world edit key to {...}.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 173), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetEditKey(trigger.GetString(0));

                return true;
            }));

            // change the world description to {...}.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 174), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetRoomDescription(trigger.GetString(0));

                return true;
            }));

            // change the world status to #.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 175), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetStatus((WorldStatus)trigger.GetInt(0));

                return true;
            }));

            // change the world curse limit to #.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 190), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetCurseLimit(trigger.GetInt(0));

                return true;
            }));

            // change the world zombie limit to #.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 191), new TriggerHandler((trigger, player, args) => {
                Room.Of(Client).SetZombieLimit(trigger.GetInt(0));

                return true;
            }));

            // place a foreground block #.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 254), new TriggerHandler((trigger, player, args) => {
                foreach (var point in trigger.Area.Points) {
                    Blocks.Of(Client).Place(point.X, point.Y, (Foreground.Id)trigger.GetInt(0));
                }

                return true;
            }));

            // place a background block #.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 255), new TriggerHandler((trigger, player, args) => {
                foreach (var point in trigger.Area.Points) {
                    Blocks.Of(Client).Place(point.X, point.Y, (Background.Id)trigger.GetInt(0));
                }

                return true;
            }));

            // place a foreground block # at (#,#).
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 256), new TriggerHandler((trigger, player, args) => {
                Blocks.Of(Client).Place(trigger.GetInt(1), trigger.GetInt(2), (Foreground.Id)trigger.GetInt(0));

                return true;
            }));

            // place a background block # at (#,#).
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 257), new TriggerHandler((trigger, player, args) => {
                Blocks.Of(Client).Place(trigger.GetInt(1), trigger.GetInt(2), (Background.Id)trigger.GetInt(0));

                return true;
            }));

            // copy the block(s) offset to (#,#).
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 258), new TriggerHandler((trigger, player, args) => {
                if (trigger.Area == null)
                    return false;

                foreach (var point in trigger.Area.Points) {
                    var block = Blocks.Of(Client).FirstOrDefault(b => b.X == point.X && b.Y == point.Y);

                    if (block.Background.Block != null) {
                        Blocks.Of(Client).Place(block.X + trigger.GetInt(0), block.Y + trigger.GetInt(1), block.Background.Block);
                    }
                    if (block.Foreground.Block != null) {
                        Blocks.Of(Client).Place(block.X + trigger.GetInt(0), block.Y + trigger.GetInt(1), block.Foreground.Block);
                    }
                }

                return true;
            }));

            // set the triggering player's variable % to %.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2000), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Set(trigger.GetVariableName(0), trigger.Get(1));

                return true;
            }));

            // set the global variable ~ to ~.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2001), new TriggerHandler((trigger, player, args) => {
                Page.SetGlobalVariable(trigger.GetVariableName(0), trigger.Get(1));

                return true;
            }));

            // take the triggering player's variable % and add # to it.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2002), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Set(trigger.GetVariableName(0), trigger.GetInt(0) + trigger.GetInt(1));

                return true;
            }));

            // take the global variable % and add # to it.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2003), new TriggerHandler((trigger, player, args) => {
                Page.SetGlobalVariable(trigger.GetVariableName(0), trigger.GetInt(0) + trigger.GetInt(1));

                return true;
            }));

            // take the triggering player's variable % and subtract # from it.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2004), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Set(trigger.GetVariableName(0), trigger.GetInt(0) - trigger.GetInt(1));

                return true;
            }));

            // take the global variable % and subtract # from it.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2005), new TriggerHandler((trigger, player, args) => {
                Page.SetGlobalVariable(trigger.GetVariableName(0), trigger.GetInt(0) - trigger.GetInt(1));

                return true;
            }));

            // take the triggering player's variable % and multiply it by #.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2006), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Set(trigger.GetVariableName(0), trigger.GetInt(0) * trigger.GetInt(1));

                return true;
            }));

            // take the global variable % and multiply it by #.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2007), new TriggerHandler((trigger, player, args) => {
                Page.SetGlobalVariable(trigger.GetVariableName(0), trigger.GetInt(0) * trigger.GetInt(1));

                return true;
            }));

            // set the triggering player's variable % to their username.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2100), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Set(trigger.GetVariableName(0), ((Player)player).Username);

                return true;
            }));

            // set the global variable ~ to the triggering player's username.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2101), new TriggerHandler((trigger, player, args) => {
                Page.SetGlobalVariable(trigger.GetVariableName(0), ((Player)player).Username);

                return true;
            }));

            // set the triggering player's variable % to the amount of gold coins they have.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2102), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Set(trigger.GetVariableName(0), ((Player)player).GoldCoins);

                return true;
            }));

            // set the global variable ~ to the amount of gold coins the triggering player has.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2103), new TriggerHandler((trigger, player, args) => {
                Page.SetGlobalVariable(trigger.GetVariableName(0), ((Player)player).GoldCoins);

                return true;
            }));

            // set the triggering player's variable % to the amount of blue coins they have.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2104), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Set(trigger.GetVariableName(0), ((Player)player).BlueCoins);

                return true;
            }));

            // set the global variable ~ to the amount of blue coins the triggering player has.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2105), new TriggerHandler((trigger, player, args) => {
                Page.SetGlobalVariable(trigger.GetVariableName(0), ((Player)player).BlueCoins);

                return true;
            }));

            // set the triggering player's variable % to the X position they're currently at.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2108), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Set(trigger.GetVariableName(0), ((Player)player).GetPhysicsPlayer().BlockX);

                return true;
            }));

            // set the global variable ~ to the X position the triggering player's currently at.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2109), new TriggerHandler((trigger, player, args) => {
                Page.SetGlobalVariable(trigger.GetVariableName(0), ((Player)player).GetPhysicsPlayer().BlockX);

                return true;
            }));

            // set the triggering player's variable % to the Y position they're currently at.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2110), new TriggerHandler((trigger, player, args) => {
                ((Player)player).Set(trigger.GetVariableName(0), ((Player)player).GetPhysicsPlayer().BlockY);

                return true;
            }));

            // set the global variable ~ to the Y position the triggering player's currently at.
            Page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 2111), new TriggerHandler((trigger, player, args) => {
                Page.SetGlobalVariable(trigger.GetVariableName(0), ((Player)player).GetPhysicsPlayer().BlockY);

                return true;
            }));

            #endregion
        }
        #endregion
    }
}