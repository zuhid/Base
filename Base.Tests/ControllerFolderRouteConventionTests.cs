using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Reflection;

namespace Zuhid.Base.Tests;

public class ControllerFolderRouteConventionTests
{
    private static ControllerModel BuildControllerModel(Type controllerType)
    {
        var model = new ControllerModel(controllerType.GetTypeInfo(), []);
        model.Selectors.Add(new SelectorModel
        {
            AttributeRouteModel = new AttributeRouteModel(new RouteAttribute("[controller]"))
        });
        return model;
    }

    [Fact]
    public void Apply_Should_PrefixRouteWithFolderName()
    {
        var model = BuildControllerModel(typeof(Fixtures.Controllers.Crm.SampleFolderedController));

        new ControllerFolderRouteConvention().Apply(model);

        Assert.Equal("crm/[controller]", model.Selectors[0].AttributeRouteModel!.Template);
    }

    [Fact]
    public void Apply_Should_LeaveRouteUnchanged_When_NoFolderBeneathControllers()
    {
        var model = BuildControllerModel(typeof(Fixtures.Controllers.SampleRootController));

        new ControllerFolderRouteConvention().Apply(model);

        Assert.Equal("[controller]", model.Selectors[0].AttributeRouteModel!.Template);
    }

    [Fact]
    public void Apply_Should_LeaveRouteUnchanged_When_ControllerDeclaresItsOwnRouteAttribute()
    {
        var model = BuildControllerModel(typeof(Fixtures.Controllers.Crm.SampleExplicitRouteController));

        new ControllerFolderRouteConvention().Apply(model);

        Assert.Equal("[controller]", model.Selectors[0].AttributeRouteModel!.Template);
    }

    [Fact]
    public void Apply_Should_AddSelector_When_ControllerHasNoSelectors()
    {
        // No selector seeded, so the convention must create one before assigning the route.
        var model = new ControllerModel(
            typeof(Fixtures.Controllers.Crm.SampleFolderedController).GetTypeInfo(), []);

        new ControllerFolderRouteConvention().Apply(model);

        Assert.Single(model.Selectors);
        Assert.Equal("crm/[controller]", model.Selectors[0].AttributeRouteModel!.Template);
    }
}
