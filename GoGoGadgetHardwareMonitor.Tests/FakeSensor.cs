// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using LibreHardwareMonitor.Hardware;

namespace GoGoGadgetHardwareMonitor.Tests;

/// <summary>
/// Minimal ISensor for settings-key tests: only Identifier, SensorType and
/// Value are real; everything else throws so a test can't silently depend on it.
/// </summary>
internal sealed class FakeSensor : ISensor
{
    public FakeSensor(SensorType type, string identifier = "/test/sensor/0", float? value = null)
    {
        SensorType = type;
        Identifier = new Identifier(identifier.Trim('/').Split('/'));
        Value = value;
    }

    public Identifier Identifier { get; }
    public SensorType SensorType { get; }
    public float? Value { get; }

    public IControl Control => throw new NotSupportedException();
    public IHardware Hardware => throw new NotSupportedException();
    public int Index => 0;
    public bool IsDefaultHidden => false;
    public float? Max => Value;
    public float? Min => Value;
    public string Name { get; set; } = "Test";
    public IReadOnlyList<IParameter> Parameters => Array.Empty<IParameter>();
    public IEnumerable<SensorValue> Values => Array.Empty<SensorValue>();
    public TimeSpan ValuesTimeWindow { get; set; }

    public void ResetMin() { }
    public void ResetMax() { }
    public void ClearValues() { }
    public void Accept(IVisitor visitor) => throw new NotSupportedException();
    public void Traverse(IVisitor visitor) => throw new NotSupportedException();
}
