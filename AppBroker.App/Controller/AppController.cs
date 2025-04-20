using AppBroker.Core.Database;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.App.Controller;
[Route("app")]
internal class AppController : ControllerBase
{
    [HttpGet]
    public Guid? GetIdByName(string name)
    {
        using var ctx = DbProvider.AppDbContext;
        return ctx.Apps.FirstOrDefault(x => EF.Functions.Like(x.Name, name))?.Id;
    }

    [HttpPatch]
    public async Task UpdateName(Guid id, string newName)
    {
        using var ctx = DbProvider.AppDbContext;

        var app = ctx.Apps.FirstOrDefault(x => x.Id == id);
        if (app is null)
        {
            ctx.Apps.Add(new Core.Database.Model.AppModel { Id = id, Name = newName });
        }
        else
        {
            app.Name = newName;
        }
        await ctx.SaveChangesAsync();
    }

    [HttpGet("settings")]
    public string? GetByKey(Guid id, string key)
    {
        using var ctx = DbProvider.AppDbContext;
        return ctx.AppConfigs.FirstOrDefault(x => x.AppId == id && x.Key == key)?.Value;
    }

    [HttpPost("settings")]
    public string? SetValue(Guid id, string key, string value)
    {
        using var ctx = DbProvider.AppDbContext;
        var setting = ctx.AppConfigs.FirstOrDefault(x => x.AppId == id && x.Key == key);
        if (setting is null)
        {
            ctx.AppConfigs.Add(new Core.Database.Model.AppConfigModel {Key = key, Value = value , AppId = id });
        }
        else
        {
            setting.Value = value;
        }
        return value;
    }
}
