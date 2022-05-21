using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using UnhollowerRuntimeLib;
using HarmonyLib;

namespace AmongUsReplay
{
    [HarmonyPatch]
    class HarmonyPatches
    {
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Awake))]
        class AwakePatch
        {
            static void Postfix()
            {
                ModManager.Instance?.ShowModStamp();
            }
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CoStartMeeting))]
        class CoStartMeetingPatch
        {
            static void Prefix(PlayerControl __instance, GameData.PlayerInfo target)
            {
                BepInExLoader.r?.setReport(__instance?.PlayerId ?? -1, target?.PlayerId ?? -1);
            }
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        class MurderPlayerPatch
        {
            static void Prefix(PlayerControl __instance, PlayerControl target)
            {
                BepInExLoader.r?.setReport(__instance?.PlayerId ?? -1, target?.PlayerId ?? -1);
            }
        }

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
        class AddChatPatch
        {
            static void Prefix(ChatController __instance, PlayerControl sourcePlayer, string chatText)
            {
                string Sender = sourcePlayer.Data.PlayerName;
                BepInExLoader.r?.AddChat(Sender, chatText);
            }
        }

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChatNote))]
        class AddChatNotePatch
        {
            static void Prefix(ChatController __instance, GameData.PlayerInfo srcPlayer, ChatNoteTypes noteType)
            {
                int remain = MeetingHud.Instance?.GetVotesRemaining() ?? 0;
                string name = srcPlayer.PlayerName ?? "";
                var lang = TranslationController.Instance?.currentLanguage;
                var IsJapanese = lang?.languageID == SupportedLangs.Japanese;
                string text;
                if(IsJapanese) text = $"{name}が投票しました。残り{remain}人";
                else text = $"{name} has voted. {remain} remaining.";
                BepInExLoader.r?.AddChat(null, text);
            }
        }
        [HarmonyPatch(typeof(SurvCamera), nameof(SurvCamera.SetAnimation))]
        class CameraPatch
        {
            static void Prefix(SurvCamera __instance, bool on)
            {
                BepInExLoader.r?.setCamera(on);
            }
        }
        [HarmonyPatch(typeof(ElectricalDoors), nameof(ElectricalDoors.Initialize))]
        class ElectricalDoorPatch
        {
            static void Postfix(ElectricalDoors __instance)
            {
                BepInExLoader.r?.setElectricalDoors(__instance);
            }
        }
        [HarmonyPatch(typeof(ElectricalDoors), nameof(ElectricalDoors.Deserialize))]
        class ElectricalDoorDeserializePatch
        {
            static void Postfix(ElectricalDoors __instance)
            {
                BepInExLoader.r?.setElectricalDoors(__instance);
            }
        }
    }
}
