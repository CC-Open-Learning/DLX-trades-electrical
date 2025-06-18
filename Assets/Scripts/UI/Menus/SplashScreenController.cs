using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    public class SplashScreenController : MenuController
    {
        /// <summary> 
        ///     Time in seconds that the loading spinner will still be animated 
        ///     while the window is fading
        ///  </summary>
        public const float AnimationDelayEndTime = 5f;

        [Tooltip("Time in seconds that the splash screen should always be displayed for")]
        public float MinimumDisplayTimeSeconds = 3f;

        [Tooltip("Angle that the loading icon rotates each iteration")]
        public float RotationSpeed = 10f;

        [Tooltip("Time in seconds between each loading icon rotation")]
        public float AnimationDelay = 0.1f;


        private VisualElement loadingIcon;

        private bool closeRequested = false;
        private bool animationMutex = false;
        private float elapsedDisplayTimeSeconds = 0f;
        
        public override bool IsOpen => Root != null && Root.ClassListContains(UIHelper.ClassSelectorVisible);

        public override void Initialize()
        {
            base.Initialize();

            loadingIcon = Root.Q<VisualElement>("LoadingIcon");
        }

        public void DelayedClose()
        {
            closeRequested = true;
            // animationMutex = false;
        }

        public void Update()
        {
            elapsedDisplayTimeSeconds += Time.unscaledDeltaTime;

            // Hide the splash screen when requested, if it has been
            // displayed for at least the minimum amount of time
            if (closeRequested && elapsedDisplayTimeSeconds >= MinimumDisplayTimeSeconds)
            {
                Close();
            }

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AnimateLoadingIcon(!animationMutex);
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                DelayedClose();
            }
#endif
        }

        public override void Open()
        {
            enabled = true;
            elapsedDisplayTimeSeconds = 0f;
            closeRequested = false;

            SetVisible(Root);

            Opened?.Invoke();
        }

        public override void Close()
        {
            enabled = false;
            animationMutex = false;

            SetHidden(Root);

            Closed?.Invoke();
        }


        public void AnimateLoadingIcon(bool running = true)
        {
            if (loadingIcon == null) { return; }

            if (running)
            {
                if (animationMutex) { return; }

                animationMutex = true;
                StartCoroutine(AnimateLoadingIconCoroutine());
                return;
            }

  //          loadingIcon.style.display = DisplayStyle.None;
            //StopAllCoroutines();
            animationMutex = false;
        }



        public IEnumerator AnimateLoadingIconCoroutine()
        {
            float fadeOutElapsed = 0f;
            loadingIcon.style.display = DisplayStyle.Flex;

            while (animationMutex)// || fadeOutElapsed < AnimationDelayEndTime)
            {
                var r = loadingIcon.worldTransform.rotation.eulerAngles;
                r.z += RotationSpeed;
                loadingIcon.transform.rotation = Quaternion.Euler(r);
                yield return new WaitForSeconds(AnimationDelay);

                if (!animationMutex) { fadeOutElapsed += Time.unscaledDeltaTime; }
            }
            loadingIcon.style.display = DisplayStyle.None;

        }

        protected void SetVisible(VisualElement element)
        {
            element.AddToClassList(UIHelper.ClassSelectorVisible);
            element.RemoveFromClassList(UIHelper.ClassSelectorHidden);
        }

        protected void SetHidden(VisualElement element)
        {
            element.AddToClassList(UIHelper.ClassSelectorHidden);
            element.RemoveFromClassList(UIHelper.ClassSelectorVisible);
        }
    }
}
