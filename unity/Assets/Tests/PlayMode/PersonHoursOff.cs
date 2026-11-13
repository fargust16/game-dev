#if UNITY_EDITOR
using GameDev.Slice.People;
using NUnit.Framework;

/// <summary>
/// The people's hours (S5c.3) stay off in the slice's play tests, which were written for a world where everyone stands
/// at the roster's place at any hour: Nell Garrow is asleep at 08:00, when every run starts. The tests of the hours
/// themselves turn them on (<see cref="GameDev.Slice.Tests.PersonHoursPlayTests"/>). Outside every namespace, so it
/// stands before every fixture of the assembly.
/// </summary>
[SetUpFixture]
public sealed class PersonHoursOff
{
    [OneTimeSetUp]
    public void Off() => PersonHours.Live = false;

    [OneTimeTearDown]
    public void On() => PersonHours.Live = true;
}
#endif
