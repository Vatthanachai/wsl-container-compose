using WslContainerCompose.Core.Compose;

namespace WslContainerCompose.Core.Tests.Compose;

public class ComposeParserTests
{
    [Fact]
    public void Parses_image_ports_environment_and_depends_on()
    {
        const string yaml = """
            services:
              web:
                image: nginx
                ports:
                  - "8080:80"
                environment:
                  - ASPNETCORE_ENVIRONMENT=Development
                depends_on:
                  - api
              api:
                image: my-api:latest
                environment:
                  LOG_LEVEL: debug
            """;

        var composeFile = ComposeParser.Parse(yaml, "myproj");

        Assert.Equal("myproj", composeFile.ProjectName);
        Assert.Equal(2, composeFile.Services.Count);

        var web = composeFile.Services["web"];
        Assert.Equal("nginx", web.Image);
        Assert.Equal([new PortMapping(8080, 80)], web.Ports);
        Assert.Equal("Development", web.Environment["ASPNETCORE_ENVIRONMENT"]);
        Assert.Equal(["api"], web.DependsOn);

        var api = composeFile.Services["api"];
        Assert.Equal("debug", api.Environment["LOG_LEVEL"]);
    }

    [Fact]
    public void Parses_bind_mount_volumes()
    {
        const string yaml = """
            services:
              db:
                image: postgres
                volumes:
                  - ./data:/var/lib/postgresql/data
            """;

        var composeFile = ComposeParser.Parse(yaml, "myproj");

        Assert.Equal([new BindMount("./data", "/var/lib/postgresql/data")], composeFile.Services["db"].Volumes);
    }

    [Fact]
    public void Rejects_named_volumes()
    {
        const string yaml = """
            services:
              db:
                image: postgres
                volumes:
                  - dbdata:/var/lib/postgresql/data
            """;

        var ex = Assert.Throws<ComposeParseException>(() => ComposeParser.Parse(yaml, "myproj"));
        Assert.Contains("named volume", ex.Message);
    }

    [Fact]
    public void Rejects_build_directive()
    {
        const string yaml = """
            services:
              api:
                build: ./api
            """;

        var ex = Assert.Throws<ComposeParseException>(() => ComposeParser.Parse(yaml, "myproj"));
        Assert.Contains("build:", ex.Message);
    }

    [Fact]
    public void Rejects_top_level_named_volumes_block()
    {
        const string yaml = """
            services:
              db:
                image: postgres
            volumes:
              dbdata: {}
            """;

        Assert.Throws<ComposeParseException>(() => ComposeParser.Parse(yaml, "myproj"));
    }

    [Fact]
    public void Parses_service_networks_list_form()
    {
        const string yaml = """
            networks:
              frontend: {}
              backend: {}
            services:
              web:
                image: nginx
                networks:
                  - frontend
              api:
                image: my-api
                networks:
                  - frontend
                  - backend
            """;

        var composeFile = ComposeParser.Parse(yaml, "myproj");

        Assert.Equal(new HashSet<string> { "frontend", "backend" }, composeFile.Networks);
        Assert.Equal(["frontend"], composeFile.Services["web"].Networks);
        Assert.Equal(["frontend", "backend"], composeFile.Services["api"].Networks);
    }

    [Fact]
    public void Parses_service_networks_map_form()
    {
        const string yaml = """
            networks:
              frontend: {}
            services:
              web:
                image: nginx
                networks:
                  frontend: {}
            """;

        var composeFile = ComposeParser.Parse(yaml, "myproj");

        Assert.Equal(["frontend"], composeFile.Services["web"].Networks);
    }

    [Fact]
    public void Service_with_no_networks_key_has_no_declared_networks()
    {
        const string yaml = """
            services:
              web:
                image: nginx
            """;

        var composeFile = ComposeParser.Parse(yaml, "myproj");

        Assert.Empty(composeFile.Services["web"].Networks);
    }

    [Fact]
    public void Rejects_service_network_not_declared_at_top_level()
    {
        const string yaml = """
            services:
              web:
                image: nginx
                networks:
                  - frontend
            """;

        var ex = Assert.Throws<ComposeParseException>(() => ComposeParser.Parse(yaml, "myproj"));
        Assert.Contains("not declared", ex.Message);
    }

    [Fact]
    public void Rejects_top_level_network_with_driver_option()
    {
        const string yaml = """
            networks:
              frontend:
                driver: overlay
            services:
              web:
                image: nginx
                networks:
                  - frontend
            """;

        var ex = Assert.Throws<ComposeParseException>(() => ComposeParser.Parse(yaml, "myproj"));
        Assert.Contains("driver", ex.Message);
    }

    [Fact]
    public void Rejects_service_network_alias()
    {
        const string yaml = """
            networks:
              frontend: {}
            services:
              web:
                image: nginx
                networks:
                  frontend:
                    aliases:
                      - web-alias
            """;

        var ex = Assert.Throws<ComposeParseException>(() => ComposeParser.Parse(yaml, "myproj"));
        Assert.Contains("aliases", ex.Message);
    }

    [Fact]
    public void Interpolates_variables_with_defaults()
    {
        const string yaml = """
            services:
              web:
                image: nginx:${TAG:-latest}
            """;

        var composeFile = ComposeParser.Parse(yaml, "myproj", new Dictionary<string, string>());

        Assert.Equal("nginx:latest", composeFile.Services["web"].Image);
    }

    [Fact]
    public void Interpolates_variables_from_environment()
    {
        const string yaml = """
            services:
              web:
                image: nginx:${TAG:-latest}
            """;

        var composeFile = ComposeParser.Parse(yaml, "myproj", new Dictionary<string, string> { ["TAG"] = "1.27" });

        Assert.Equal("nginx:1.27", composeFile.Services["web"].Image);
    }
}
