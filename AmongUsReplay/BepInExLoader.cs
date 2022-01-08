using BepInEx;
using UnhollowerRuntimeLib;
using HarmonyLib;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace AmongUsReplay
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    [BepInProcess("Among Us.exe")]
    //[BepInDependency("me.eisbison.theotherroles",BepInDependency.DependencyFlags.SoftDependency)]
    public class BepInExLoader : BepInEx.IL2CPP.BasePlugin
    {
        public const string
            MODNAME = "AmongUsReplay",
            AUTHOR = "sawa9090",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.0.0.0";

        public static BepInEx.Logging.ManualLogSource log;
        public BepInExLoader()
        {
            log = Log;
        }

        public override void Load()
        {
            log.LogMessage("Loaded");
            try
            {
                var harmony = new Harmony(AUTHOR + "." + MODNAME + ".il2cpp");
                harmony.PatchAll();

            }
            catch
            {
                log.LogError("Harmony Failed");
            }
            ReadSpacePtr = Marshal.AllocHGlobal(ReadSpaceSize);
            Marshal.Copy(new byte[8] { 0, 0xFF, 0xFF, 0, 0, 0, 0, 0 }, 0, ReadSpacePtr, 8);
            Marshal.Copy(new byte[16] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, 0, ReadSpacePtr + 0x1E08, 16);
            Marshal.Copy(new byte[4], 0, ReadSpacePtr + 0x1E18, 4);
            r = new ReadSpace(ReadSpacePtr);
            var app = new ProcessStartInfo();
            app.FileName = System.IO.Path.GetDirectoryName(Application.dataPath) + @"\AmongUsReplayInWindow\AmongUsReplayInWindow.exe";
            app.Arguments = ((long)ReadSpacePtr).ToString();
            app.UseShellExecute = true;
            replayInWindow = Process.Start(app);
        }


        public override bool Unload()
        {
            if (replayInWindow != null && !replayInWindow.HasExited)
            {
                replayInWindow.CloseMainWindow();
                if (!replayInWindow.WaitForExit(3000)) replayInWindow.Kill();
            }
            r = null;
            Marshal.FreeHGlobal(ReadSpacePtr);
            return true;
        }

        static IntPtr ReadSpacePtr;
        static int ReadSpaceSize = 0x2000;
        public static ReadSpace r = null;
        Process replayInWindow;
        public class ReadSpace
        {
            IntPtr ReadSpacePtr;
            byte MeetingNum;
            byte ChatNum;
            byte ElectricalDoorNum;

            /*
                0x00 byte   MeetingNum
                0x01 sbyte  Reporter
                0x02 sbyte  ReportTarget
                0x03 byte   ChatNum
                0x04 bool   CameraInUse
                0x08~0x1E07 Chat[15](size = 0x200,{0x00 int SenderLength, 0x4 bytes[<0x30] SenderBytes, 0x34 int textLength, 0x38 byte[<0x1C8] TextBytes})
                0x1E08  sbyte[] Murder[16]
                0x1E18  uint door
             */

            public ReadSpace(IntPtr ptr)
            {
                ReadSpacePtr = ptr;
                MeetingNum = 0;
                ChatNum = 0;
                ElectricalDoorNum = 0;
                log?.LogMessage($"{(int)ptr:X}");
            }
            public bool AddChat(string Sender, string chatText)
            {
                IntPtr chatPtr = ReadSpacePtr + 0x8 + 0x200 * ChatNum;

                if (Sender == null) Marshal.Copy(BitConverter.GetBytes(-1), 0, chatPtr, 4);
                else
                {
                    byte[] SenderText = System.Text.Encoding.UTF8.GetBytes(Sender);
                    int senderLen = SenderText.Length;
                    if (senderLen > 0x30) senderLen = 0x30;
                    Marshal.Copy(BitConverter.GetBytes(senderLen), 0, chatPtr, 4);
                    Marshal.Copy(SenderText, 0, chatPtr + 4, senderLen);
                }
                if (chatText == null) chatText = "";
                byte[] text = System.Text.Encoding.UTF8.GetBytes(chatText);
                int len = text.Length;
                if (len > 0x1C8) len = 0x1C8;
                Marshal.Copy(BitConverter.GetBytes(len), 0, chatPtr + 0x34, 4);
                Marshal.Copy(text, 0, chatPtr + 0x38, len);

                ChatNum = (byte)((ChatNum + 1) % 15);
                Marshal.Copy(new byte[1] { ChatNum }, 0, ReadSpacePtr + 3, 1);
                return true;
            }

            public bool setReport(int reporter,int target)
            {
                MeetingNum++;
                byte[] bytes = new byte[3] { MeetingNum, getIdByte(reporter), getIdByte(target) };
                Marshal.Copy(bytes, 0, ReadSpacePtr, 3);
                return true;
            }

            public bool setMurder(int murder, int target)
            {
                if (isIdRange(target))
                {
                    Marshal.WriteByte(ReadSpacePtr + 0x1E08 + target, getIdByte(murder));
                    return true;
                }else return false;

            }
            public bool setCamera(bool on)
            {
                if (on) Marshal.WriteByte(ReadSpacePtr + 4, 1);
                else Marshal.WriteByte(ReadSpacePtr + 4, 0);
                return true;
            }
            uint electricalDoorsInt = 0;
            public bool setElectricalDoors(ElectricalDoors doors)
            {
                var lists = doors.Doors;
                if (lists.Count > 32) return false;
                electricalDoorsInt = 0;
                uint one = 1;
                for (int i = 0; i < lists.Count; i++)
                {
                    if (!lists[i].IsOpen)
                        electricalDoorsInt |= one << i;
                }
                ElectricalDoorNum++;
                var bytes = BitConverter.GetBytes(electricalDoorsInt);
                bytes[3] = ElectricalDoorNum;
                Marshal.Copy(bytes, 0, ReadSpacePtr + 0x1E18, 4);
                return true;
            }
            private byte getIdByte(int id)
            {
                return (byte)(isIdRange(id) ? id : 0xFF);
            }
            private bool isIdRange(int id)
            {
                return (id >= 0 && id < 15);
            }
        }
    }
}
