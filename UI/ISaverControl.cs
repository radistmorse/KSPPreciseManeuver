using System.Collections.Generic;

namespace KSPPreciseManeuver.UI {
  public interface ISaverControl {
    string CanvasName { get; }
    List<string> presetNames { get; }

    void AddPreset (string name);
    void RemovePreset (string name);
    void loadPreset (string name);
    string suggestPresetName ();
  }
}