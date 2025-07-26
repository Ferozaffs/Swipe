# Swipe
## Summary
A macro tool that is using some custom hardware, also works with keyboard.

## Gesture
Through the "Band" invoke different functions through gestures. Using dynamic time warping to match the different acceleration curves.

Hardware is based on the very small [CodeCell](https://microbots.io/products/codecell?variant=49714638717261)
DTW implementation made by https://github.com/kkartavenka/FastDtw.CSharp

## Macro pad
The macro pad is a bluetooth numpad based on the [DumbPad](https://github.com/imchipwood/dumbpad) platform. Changes have been made to enable hot-swappable keys an ESP32 controller.
