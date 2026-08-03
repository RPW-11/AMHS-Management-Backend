using Application.Common.Errors;
using Application.Common.Interfaces.RoutePlanning;
using FluentResults;
using SkiaSharp;

namespace Infrastructure.RoutePlanning.Rgv;

/// <summary>
/// Validates uploaded layout images with the same library that later draws on them, so anything
/// accepted here is guaranteed to be decodable by <see cref="RouteDrawer"/>.
/// </summary>
public sealed class SkiaSourceImageValidator : ISourceImageValidator
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
    };

    private static readonly HashSet<SKEncodedImageFormat> AllowedFormats =
    [
        SKEncodedImageFormat.Png,
        SKEncodedImageFormat.Jpeg,
        SKEncodedImageFormat.Webp,
    ];

    private const string AllowedFormatsDescription = "PNG, JPEG and WebP";

    /// <summary>
    /// Ceiling on decoded pixels, checked from the header before anything is decoded. A small
    /// compressed file can expand enormously, so without this the full decode below would be an
    /// easy way to exhaust memory on the request thread.
    /// </summary>
    private const long MaxPixels = 30_000_000;

    public Result Validate(byte[] imageBytes, string? contentType)
    {
        if (imageBytes.Length == 0)
        {
            return Result.Fail(ApplicationError.Validation("The source layout image is empty"));
        }

        string declaredType = contentType?.Split(';')[0].Trim() ?? string.Empty;

        if (!AllowedContentTypes.Contains(declaredType))
        {
            return Result.Fail(ApplicationError.Validation(
                $"The source layout image must be one of {AllowedFormatsDescription}, but '{declaredType}' was uploaded"));
        }

        using var stream = new MemoryStream(imageBytes);
        using var codec = SKCodec.Create(stream);

        if (codec is null)
        {
            return Result.Fail(ApplicationError.Validation(
                "The source layout image could not be read, it may be corrupt or incompletely uploaded"));
        }

        if (!AllowedFormats.Contains(codec.EncodedFormat))
        {
            return Result.Fail(ApplicationError.Validation(
                $"The source layout image must be one of {AllowedFormatsDescription}, but the uploaded file is {codec.EncodedFormat}"));
        }

        if (codec.Info.Width <= 0 || codec.Info.Height <= 0)
        {
            return Result.Fail(ApplicationError.Validation("The source layout image has no usable dimensions"));
        }

        long pixels = (long)codec.Info.Width * codec.Info.Height;
        if (pixels > MaxPixels)
        {
            return Result.Fail(ApplicationError.Validation(
                $"The source layout image is {codec.Info.Width}x{codec.Info.Height}, larger than the {MaxPixels:N0} pixel limit"));
        }

        var decodeInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(decodeInfo);
        var decodeResult = codec.GetPixels(decodeInfo, bitmap.GetPixels());

        if (decodeResult != SKCodecResult.Success)
        {
            return Result.Fail(ApplicationError.Validation(
                $"The source layout image could not be fully decoded ({decodeResult}), it may be corrupt or incompletely uploaded"));
        }

        return Result.Ok();
    }
}
