using System;

public static class CameraEvents
{
    // Anyone can "shout" these, and anyone can "listen" to them.
    public static Action OnManualPanBegan;
    public static Action OnRecenterRequested;
}
