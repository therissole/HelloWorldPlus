using System.Diagnostics;
using HelloWorldPlus.Web.Infrastructure;

namespace HelloWorldPlus.WebTests.Infrastructure;

[TestFixture]
public sealed class BrowserTelemetryTests
{
    [Test]
    public void NameCurrentAction_ReplacesTheActiveActivityDisplayName()
    {
        using var activity = new Activity("framework-generated event");
        activity.Start();

        BrowserTelemetry.NameCurrentAction("Create language clicked");

        Assert.That(activity.DisplayName, Is.EqualTo("Create language clicked"));
    }
}
