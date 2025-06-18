using UnityEngine;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    public class BoxMountingFeedback
    {
        public const string FeedbackIndicatorBoxId = "IndicatorBox";
        public const string FeedbackIndicatorLocationId = "IndicatorLocation";

        public const string FeedbackIndicatorClass = "feedback-indicator";
        public const string FeedbackIndicatorCorrectClassModifier = FeedbackIndicatorClass + "__correct";
        public const string FeedbackIndicatorIncorrectClassModifier = FeedbackIndicatorClass + "__incorrect";


        public Task Task;

        // Can probably track these in a different way
        public bool BoxCorrect;
        public bool LocationCorrect;

        public VisualElement RootElement;
        public VisualElement BoxFeedbackIndicator;
        public VisualElement LocationFeedbackIndicator;

        public BoxMountingFeedback(VisualElement element, Task task) 
        {
            Task = task;
            RootElement = element;

            if (RootElement == null)
            {
                Debug.LogWarning("No VisualElement provided for BoxMountingFeedback");
                return;
            }

            BoxFeedbackIndicator = element.Q<VisualElement>(FeedbackIndicatorBoxId);
            LocationFeedbackIndicator = element.Q<VisualElement>(FeedbackIndicatorLocationId);
        }

        public void Update(bool box, bool location)
        {
            BoxCorrect = box;
            LocationCorrect = location;

            if (BoxFeedbackIndicator != null)
            {
                SetIndicator(BoxFeedbackIndicator, BoxCorrect);
            }

            if (LocationFeedbackIndicator != null)
            {
                SetIndicator(LocationFeedbackIndicator, LocationCorrect);
            }
        }

        private void SetIndicator(VisualElement indicator, bool correct)
        {
            indicator.RemoveFromClassList(FeedbackIndicatorIncorrectClassModifier);
            indicator.RemoveFromClassList(FeedbackIndicatorCorrectClassModifier);
            indicator.AddToClassList(correct ? FeedbackIndicatorCorrectClassModifier : FeedbackIndicatorIncorrectClassModifier);
        }

    }
}
