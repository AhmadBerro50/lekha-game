#import <UIKit/UIKit.h>

// iOS Haptic Feedback using UIFeedbackGenerator APIs
// These provide subtle, Apple-standard haptic taps — not the jarring Handheld.Vibrate()

static UIImpactFeedbackGenerator *lightGenerator = nil;
static UIImpactFeedbackGenerator *mediumGenerator = nil;
static UIImpactFeedbackGenerator *heavyGenerator = nil;
static UISelectionFeedbackGenerator *selectionGenerator = nil;
static UINotificationFeedbackGenerator *notificationGenerator = nil;

extern "C" {

    void _LekhaHaptics_Prepare() {
        if (@available(iOS 10.0, *)) {
            if (lightGenerator == nil) {
                lightGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
                mediumGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
                heavyGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
                selectionGenerator = [[UISelectionFeedbackGenerator alloc] init];
                notificationGenerator = [[UINotificationFeedbackGenerator alloc] init];
            }
            [lightGenerator prepare];
            [mediumGenerator prepare];
            [selectionGenerator prepare];
        }
    }

    // Very subtle tap — card selection, hover
    void _LekhaHaptics_SelectionTap() {
        if (@available(iOS 10.0, *)) {
            if (selectionGenerator == nil) _LekhaHaptics_Prepare();
            [selectionGenerator selectionChanged];
            [selectionGenerator prepare];
        }
    }

    // Light tap — card play, UI button
    void _LekhaHaptics_LightImpact() {
        if (@available(iOS 10.0, *)) {
            if (lightGenerator == nil) _LekhaHaptics_Prepare();
            [lightGenerator impactOccurred];
            [lightGenerator prepare];
        }
    }

    // Medium tap — trick win, emoji
    void _LekhaHaptics_MediumImpact() {
        if (@available(iOS 10.0, *)) {
            if (mediumGenerator == nil) _LekhaHaptics_Prepare();
            [mediumGenerator impactOccurred];
            [mediumGenerator prepare];
        }
    }

    // Heavy tap — special card (QoS, 10D)
    void _LekhaHaptics_HeavyImpact() {
        if (@available(iOS 10.0, *)) {
            if (heavyGenerator == nil) _LekhaHaptics_Prepare();
            [heavyGenerator impactOccurred];
            [heavyGenerator prepare];
        }
    }

    // Success notification — game win
    void _LekhaHaptics_NotificationSuccess() {
        if (@available(iOS 10.0, *)) {
            if (notificationGenerator == nil) _LekhaHaptics_Prepare();
            [notificationGenerator notificationOccurred:UINotificationFeedbackTypeSuccess];
            [notificationGenerator prepare];
        }
    }

    // Error notification — game lose
    void _LekhaHaptics_NotificationError() {
        if (@available(iOS 10.0, *)) {
            if (notificationGenerator == nil) _LekhaHaptics_Prepare();
            [notificationGenerator notificationOccurred:UINotificationFeedbackTypeError];
            [notificationGenerator prepare];
        }
    }
}
