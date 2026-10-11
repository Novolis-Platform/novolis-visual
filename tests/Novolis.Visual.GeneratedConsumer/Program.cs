if (Generated.Consumer.GeneratedSurfaceProgram.EvaluateScalar(0, 0, 0) != 5d)
{
    return 1;
}

var sprite = Generated.Consumer.GeneratedImages.Assets[0];
if (sprite.Width != 1 ||
    sprite.Height != 1 ||
    sprite.Pixels.Length != 4 ||
    sprite.Pixels[0] != 0xFF ||
    sprite.Frames[0].Name != "frame-0000")
{
    return 2;
}

return 0;
