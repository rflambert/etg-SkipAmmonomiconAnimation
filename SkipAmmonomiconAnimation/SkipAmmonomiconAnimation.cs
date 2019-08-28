using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoMod.RuntimeDetour;
using System.Reflection;
using UnityEngine;

namespace SkipAmmonomiconAnimation
{
    public class SkipAmmonomiconAnimation : ETGModule
    {
        static MethodInfo SetFrame = typeof(AmmonomiconController).GetMethod("SetFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        static FieldInfo m_AmmonomiconInstance = typeof(AmmonomiconController).GetField("m_AmmonomiconInstance", BindingFlags.Instance | BindingFlags.NonPublic);
        static FieldInfo m_isPageTransitioning = typeof(AmmonomiconController).GetField("m_isPageTransitioning", BindingFlags.Instance | BindingFlags.NonPublic);
        static MethodInfo HandleQueuedUnlocks = typeof(AmmonomiconController).GetMethod("HandleQueuedUnlocks", BindingFlags.Instance | BindingFlags.NonPublic);

        public override void Init()
        {
        }

        public override void Start()
        {
            ETGModConsole.Log("SkipAmmonomiconAnimation v1.0.0 initialised.");
            Hook AnimationHook = new Hook(
                typeof(AmmonomiconController).GetMethod("HandleOpenAmmonomicon", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(SkipAmmonomiconAnimation).GetMethod("HandleOpenAmmonomiconHook"));
        }

        public override void Exit()
        {
        }

        public static IEnumerator HandleOpenAmmonomiconHook(Action<AmmonomiconController, bool, bool, EncounterTrackable> orig, AmmonomiconController self, bool isDeath, bool isShortAnimation, EncounterTrackable targetTrackable = null)
        {
            if (!isShortAnimation || isDeath) orig(self, isDeath, isShortAnimation, targetTrackable = null);
            else
            {
                List<AmmonomiconFrameDefinition> TargetAnimationFrames = self.OpenAnimationFrames;
                SetFrame.Invoke(self, new object[] { TargetAnimationFrames[TargetAnimationFrames.Count - 1] });
                ((AmmonomiconInstanceManager)m_AmmonomiconInstance.GetValue(self)).Open();
                if (targetTrackable != null)
                {
                    AmmonomiconPokedexEntry pokedexEntry = self.CurrentLeftPageRenderer.GetPokedexEntry(targetTrackable);
                    if (pokedexEntry != null)
                    {
                        Debug.Log("GET INFO SUCCESS");
                        pokedexEntry.ForceFocus();
                    }
                }
                m_isPageTransitioning.SetValue(self, false);
                HandleQueuedUnlocks.Invoke(self, new object[] { });
                yield break;
            }
        }
    }
}
