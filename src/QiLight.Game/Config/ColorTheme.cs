using Microsoft.Xna.Framework;

namespace QiLight.Game.Config;

public record ColorTheme(
    string Name,
    Color Border,
    Color Trail,
    Color Qix,
    Color Sparx,
    Color CapturedFill,
    Color Background,
    Color HUD
)
{
    public static readonly ColorTheme Synthwave = new(
        "Synthwave",
        Border: new Color(255, 0, 255),
        Trail: new Color(0, 255, 255),
        Qix: new Color(255, 100, 255),
        Sparx: new Color(255, 255, 0),
        CapturedFill: new Color(128, 0, 128, 60),
        Background: new Color(10, 0, 20),
        HUD: new Color(0, 255, 255)
    );

    public static readonly ColorTheme Toxic = new(
        "Toxic",
        Border: new Color(0, 255, 0),
        Trail: new Color(200, 255, 0),
        Qix: new Color(0, 255, 100),
        Sparx: new Color(255, 255, 0),
        CapturedFill: new Color(0, 80, 0, 60),
        Background: new Color(0, 10, 0),
        HUD: new Color(200, 255, 0)
    );

    public static readonly ColorTheme Ember = new(
        "Ember",
        Border: new Color(255, 100, 0),
        Trail: new Color(255, 200, 0),
        Qix: new Color(255, 50, 0),
        Sparx: new Color(255, 255, 100),
        CapturedFill: new Color(80, 20, 0, 60),
        Background: new Color(20, 5, 0),
        HUD: new Color(255, 200, 0)
    );

    public static readonly ColorTheme Ice = new(
        "Ice",
        Border: new Color(100, 200, 255),
        Trail: new Color(200, 240, 255),
        Qix: new Color(0, 150, 255),
        Sparx: new Color(255, 255, 255),
        CapturedFill: new Color(0, 40, 80, 60),
        Background: new Color(0, 5, 15),
        HUD: new Color(200, 240, 255)
    );

    public static readonly ColorTheme[] AllThemes = { Synthwave, Toxic, Ember, Ice };
}
