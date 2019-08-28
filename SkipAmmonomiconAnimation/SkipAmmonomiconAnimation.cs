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
        static MethodInfo GetAnimationLength = typeof(AmmonomiconController).GetMethod("GetAnimationLength", BindingFlags.Instance | BindingFlags.NonPublic);
        static FieldInfo GLOBAL_ANIMATION_SCALE = typeof(AmmonomiconController).GetField("GLOBAL_ANIMATION_SCALE", BindingFlags.Instance | BindingFlags.NonPublic);
        static MethodInfo SetFrame = typeof(AmmonomiconController).GetMethod("SetFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        static FieldInfo m_applicationFocus = typeof(AmmonomiconController).GetField("m_applicationFocus", BindingFlags.Instance | BindingFlags.NonPublic);
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

        public static IEnumerator HandleOpenAmmonomiconHook(Action<AmmonomiconController, bool, bool, EncounterTrackable> orig, AmmonomiconController self, bool isDeath, bool isShortAnimation, EncounterTrackable targetTrackable)
        {
            List<AmmonomiconFrameDefinition> TargetAnimationFrames = self.OpenAnimationFrames;

            if (!isShortAnimation || isDeath)
            {
                if (isShortAnimation)
                {
                    TargetAnimationFrames = new List<AmmonomiconFrameDefinition>();
                    for (int i = 0; i < 9; i++)
                    {
                        TargetAnimationFrames.Add(self.OpenAnimationFrames[i]);
                    }
                    for (int j = 23; j < self.OpenAnimationFrames.Count; j++)
                    {
                        TargetAnimationFrames.Add(self.OpenAnimationFrames[j]);
                    }
                    AkSoundEngine.PostEvent("Play_UI_ammonomicon_open_01", self.gameObject);
                }
                else AkSoundEngine.PostEvent("Play_UI_ammonomicon_intro_01", self.gameObject);

                float animationTime = (float)GetAnimationLength.Invoke(self, new object[] { TargetAnimationFrames });
                float elapsed = 0f;
                int currentFrameIndex = 0;
                float nextFrameTime = TargetAnimationFrames[0].frameTime * (float)GLOBAL_ANIMATION_SCALE.GetValue(self);
                SetFrame.Invoke(self, new object[] { TargetAnimationFrames[0] });
                while (elapsed < animationTime)
                {
                    elapsed += GameManager.INVARIANT_DELTA_TIME;
                    if (elapsed >= animationTime)
                    {
                        break;
                    }
                    if (elapsed >= nextFrameTime)
                    {
                        currentFrameIndex++;
                        nextFrameTime += TargetAnimationFrames[currentFrameIndex].frameTime * (float)GLOBAL_ANIMATION_SCALE.GetValue(self);
                        SetFrame.Invoke(self, new object[] { TargetAnimationFrames[currentFrameIndex] });
                    }
                    while (!((bool)m_applicationFocus.GetValue(self)))
                    {
                        yield return null;
                    }
                    yield return null;
                }
            }

            SetFrame.Invoke(self, new object[] { TargetAnimationFrames[TargetAnimationFrames.Count - 1] });
            if (isDeath) ((AmmonomiconInstanceManager)m_AmmonomiconInstance.GetValue(self)).OpenDeath();
            else ((AmmonomiconInstanceManager)m_AmmonomiconInstance.GetValue(self)).Open();

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
