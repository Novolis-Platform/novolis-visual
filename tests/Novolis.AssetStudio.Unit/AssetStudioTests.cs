using System.Text;
using Novolis.AssetStudio.Core;
using Novolis.AssetStudio.Export;
using Novolis.AssetStudio.Import;
using Novolis.Visual.Baking;
using Novolis.Visual.CodeGen;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.AssetStudio.Unit;

public sealed class AssetStudioTests
{
    [Test]
    public async Task Commands_support_undo_and_redo()
    {
        var document = AssetStudioDocument.Create(CreateRecipe());
        var added = VisualNode.Create(
            Node("00000000000000000000000000000002"),
            VisualBuiltInNodeTypes.ConstantScalar,
            1,
            [
                new KeyValuePair<string, VisualValue>(
                    "value",
                    new ScalarValue(3)),
            ]);
        var session = AssetStudioSessionState.Create(document)
            .Apply(new AddNodeCommand(added));

        await Assert.That(session.Document.Recipe.Nodes.Length).IsEqualTo(2);
        await Assert.That(session.UndoCount).IsEqualTo(1);

        var undone = session.Undo();
        await Assert.That(undone.Document.Recipe.Nodes.Length).IsEqualTo(1);
        await Assert.That(undone.RedoCount).IsEqualTo(1);

        var redone = undone.Redo();
        await Assert.That(redone.Document.Recipe.Nodes.Length).IsEqualTo(2);
        await Assert.That(redone.Document.Recipe.SemanticFingerprint())
            .IsEqualTo(session.Document.Recipe.SemanticFingerprint());
    }

    [Test]
    public async Task Latest_compile_result_wins_when_requests_finish_out_of_order()
    {
        var firstGate = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var secondGate = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new AssetStudioCompileCoordinator();
        var firstDocument = AssetStudioDocument.Create(
            CreateRecipe("first"));
        var secondDocument = AssetStudioDocument.Create(
            CreateRecipe("second"));

        static async ValueTask<Novolis.Visual.Compile.VisualIR> Compile(
            AssetStudioCompileRequest request,
            CancellationToken cancellationToken,
            TaskCompletionSource<object?> firstGate,
            TaskCompletionSource<object?> secondGate)
        {
            var gate = request.Document.Recipe.RecipeId == "first"
                ? firstGate
                : secondGate;
            await gate.Task.WaitAsync(cancellationToken);
            return Novolis.Visual.Compile.VisualCompiler.Compile(
                request.Document.Recipe,
                request.Options);
        }

        var first = coordinator.SubmitAsync(
            firstDocument,
            (request, cancellationToken) =>
                Compile(request, cancellationToken, firstGate, secondGate));
        var second = coordinator.SubmitAsync(
            secondDocument,
            (request, cancellationToken) =>
                Compile(request, cancellationToken, firstGate, secondGate));

        secondGate.SetResult(null);
        var secondResult = await second;
        await Assert.That(secondResult.IsLatest).IsTrue();
        await Assert.That(coordinator.LatestResult?.RequestId)
            .IsEqualTo(secondResult.RequestId);

        firstGate.SetResult(null);
        var firstResult = await first;
        await Assert.That(firstResult.IsLatest).IsFalse();
        await Assert.That(coordinator.LatestResult?.RequestId)
            .IsEqualTo(secondResult.RequestId);
    }

    [Test]
    public async Task Persistence_is_canonical_and_excludes_view_state()
    {
        var document = AssetStudioDocument.Create(CreateRecipe());
        var view = new AssetStudioViewState(
            new VisualEditorViewState(
                [new KeyValuePair<NodeId, VisualPoint>(
                    Node("00000000000000000000000000000001"),
                    new VisualPoint(12, 20))],
                [Node("00000000000000000000000000000001")],
                1.25));
        var viewed = document.WithView(view);

        var json = AssetStudioDocumentPersistence.ToCanonicalJson(viewed);
        var restored = AssetStudioDocumentPersistence.FromJson(json);

        await Assert.That(json).DoesNotContain("view");
        await Assert.That(restored.Recipe.SemanticFingerprint())
            .IsEqualTo(document.Recipe.SemanticFingerprint());
        await Assert.That(viewed.ViewRevision).IsGreaterThan(document.ViewRevision);
        await Assert.That(restored.View.SelectedNodes).IsEmpty();
    }

    [Test]
    public async Task Import_rejects_unsafe_paths_and_oversized_payloads()
    {
        var json = Encoding.UTF8.GetBytes(
            AssetStudioDocumentPersistence.ToCanonicalJson(
                AssetStudioDocument.Create(CreateRecipe())));
        var unsafePath = AssetStudioJsonImporter.Import(
            new AssetStudioImportRequest("../recipe.json", json));
        var oversized = AssetStudioJsonImporter.Import(
            new AssetStudioImportRequest(
                "recipe.json",
                json,
                new AssetStudioImportLimits
                {
                    MaxPayloadBytes = json.Length - 1,
                }));

        await Assert.That(unsafePath.IsSuccess).IsFalse();
        await Assert.That(unsafePath.Diagnostics[0].Code).IsEqualTo("AST101");
        await Assert.That(oversized.IsSuccess).IsFalse();
        await Assert.That(oversized.Diagnostics[0].Code).IsEqualTo("AST102");
    }

    [Test]
    public async Task Exports_are_deterministic_and_code_is_closed()
    {
        var document = AssetStudioDocument.Create(CreateRecipe());
        var request = new AssetStudioExportRequest(
            document,
            AssetStudioExportProfile.Code);
        var first = new AssetStudioExporter().Export(request);
        var second = new AssetStudioExporter().Export(request);

        await Assert.That(first.Success).IsTrue();
        await Assert.That(first.Report.CodeOnlyClosed).IsTrue();
        await Assert.That(first.Manifest.ToCanonicalJson())
            .IsEqualTo(second.Manifest.ToCanonicalJson());
        await Assert.That(first.Artifacts.Length)
            .IsEqualTo(second.Artifacts.Length);
        for (var index = 0; index < first.Artifacts.Length; index++)
        {
            await Assert.That(first.Artifacts[index].LogicalPath)
                .IsEqualTo(second.Artifacts[index].LogicalPath);
            await Assert.That(first.Artifacts[index].Sha256)
                .IsEqualTo(second.Artifacts[index].Sha256);
        }
    }

    [Test]
    public async Task Every_export_profile_produces_bounded_artifacts()
    {
        var document = AssetStudioDocument.Create(CreateRecipe());
        var options = new AssetStudioExportOptions
        {
            Bake = new VisualBakeOptions
            {
                Width = 2,
                Height = 2,
            },
        };
        var exporter = new AssetStudioExporter();

        foreach (var profile in Enum.GetValues<AssetStudioExportProfile>())
        {
            var result = exporter.Export(
                new AssetStudioExportRequest(document, profile, options));
            await Assert.That(result.Success).IsTrue();
            await Assert.That(result.Artifacts).IsNotEmpty();
            await Assert.That(result.Manifest.Entries.Length)
                .IsEqualTo(result.Artifacts.Length);
        }
    }

    [Test]
    public async Task Image_import_atlas_and_embedded_code_are_deterministic()
    {
        var source = new VisualBakeImage
        {
            Width = 2,
            Height = 2,
            Pixels =
            [
                new VisualRgba32(255, 0, 0, 255),
                new VisualRgba32(0, 255, 0, 255),
                new VisualRgba32(0, 0, 255, 255),
                new VisualRgba32(255, 255, 255, 255),
            ],
        }.ToPng();
        var imported = AssetStudioImageImporter.Import(
            new AssetStudioImageImportRequest("sprites/icon.png", source));

        await Assert.That(imported.IsSuccess).IsTrue();
        var asset = imported.Asset!;
        await Assert.That(asset.Width).IsEqualTo(2);
        await Assert.That(asset.Height).IsEqualTo(2);
        await Assert.That(asset.Frames.Length).IsEqualTo(1);

        var firstAtlas = new VisualSpriteAtlasPacker().Bake([asset]);
        var secondAtlas = new VisualSpriteAtlasPacker().Bake([asset]);
        await Assert.That(firstAtlas.PngBytes)
            .IsEquivalentTo(secondAtlas.PngBytes);
        await Assert.That(firstAtlas.Frames[0].SourceFrame)
            .IsEqualTo("frame-0000");

        var generated = new VisualImageCodeGenerator().Generate(
            [asset],
            "Generated.Tests",
            "GeneratedSprites");
        await Assert.That(CodeOnlyClosureVerifier.Verify([generated]).IsClosed)
            .IsTrue();
        await Assert.That(generated.Source).Contains("GeneratedSprites");

        var options = new AssetStudioExportOptions
        {
            Assets = [asset],
            Bake = new VisualBakeOptions
            {
                Target = VisualBakeTarget.SpriteAtlas,
                Width = 2,
                Height = 2,
            },
        };
        var codeExport = new AssetStudioExporter().Export(
            new AssetStudioExportRequest(
                AssetStudioDocument.Create(CreateRecipe()),
                AssetStudioExportProfile.Code,
                options));
        var bakeExport = new AssetStudioExporter().Export(
            new AssetStudioExportRequest(
                AssetStudioDocument.Create(CreateRecipe()),
                AssetStudioExportProfile.Bake,
                options));
        await Assert.That(codeExport.Success).IsTrue();
        await Assert.That(codeExport.Artifacts.Select(artifact => artifact.LogicalPath))
            .Contains("code/GeneratedAssetImages.cs");
        await Assert.That(bakeExport.Artifacts.Select(artifact => artifact.LogicalPath))
            .Contains("bake/sprite-atlas.png");
        await Assert.That(bakeExport.Artifacts.Select(artifact => artifact.LogicalPath))
            .Contains("bake/sprite-atlas.json");
    }

    [Test]
    public async Task Image_import_rejects_unsafe_paths_and_payloads_before_decoding()
    {
        var source = new VisualBakeImage
        {
            Width = 1,
            Height = 1,
            Pixels = [new VisualRgba32(1, 2, 3, 4)],
        }.ToPng();
        var unsafePath = AssetStudioImageImporter.Import(
            new AssetStudioImageImportRequest("../sprite.png", source));
        var oversized = AssetStudioImageImporter.Import(
            new AssetStudioImageImportRequest(
                "sprite.png",
                source,
                limits: new AssetStudioImageImportLimits
                {
                    MaxPayloadBytes = source.Length - 1,
                }));

        await Assert.That(unsafePath.IsSuccess).IsFalse();
        await Assert.That(unsafePath.Diagnostics[0].Code).IsEqualTo("ASI101");
        await Assert.That(oversized.IsSuccess).IsFalse();
        await Assert.That(oversized.Diagnostics[0].Code).IsEqualTo("ASI103");
    }

    [Test]
    public async Task Effect_values_round_trip_through_canonical_recipe_json()
    {
        var node = NodeId.Parse("00000000000000000000000000000009");
        var emitter = new EmitterValue(
            "sparks",
            42,
            5,
            2,
            System.Numerics.Vector3.UnitY,
            new ColorValue(1, 0.5, 0.1));
        var recipe = new VisualRecipe(
            "effects",
            [
                VisualNode.Create(
                    node,
                    VisualBuiltInNodeTypes.ConstantEmitter,
                    1,
                    [new KeyValuePair<string, VisualValue>("value", emitter)]),
            ],
            outputs:
            [
                new VisualOutput(
                    "emitter",
                    "Emitter",
                    new VisualPortReference(node, new PortId("value")),
                    emitter.Contract),
            ]);

        var json = VisualRecipeSerializer.ToCanonicalJson(recipe);
        var restored = VisualRecipeSerializer.Deserialize(
            json,
            VisualCompiler.CreateDefaultRegistry());
        await Assert.That(restored.SemanticFingerprint())
            .IsEqualTo(recipe.SemanticFingerprint());
        await Assert.That(restored.Nodes[0].Values["value"])
            .IsTypeOf<EmitterValue>();
        var compilation = VisualCompiler.Compile(restored);
        await Assert.That(compilation.Success).IsTrue();
        var evaluated = Novolis.Visual.Evaluation.VisualEvaluator.EvaluateOutput(
            compilation,
            "emitter");
        await Assert.That(evaluated).IsTypeOf<EmitterValue>();
    }

    private static VisualRecipe CreateRecipe(string recipeId = "asset")
    {
        var node = VisualNode.Create(
            Node("00000000000000000000000000000001"),
            VisualBuiltInNodeTypes.ConstantScalar,
            1,
            [
                new KeyValuePair<string, VisualValue>(
                    "value",
                    new ScalarValue(2)),
            ]);
        var contract = new VisualTypeContract(
            VisualValueKind.Scalar,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);
        return new VisualRecipe(
            recipeId,
            [node],
            outputs:
            [
                new VisualOutput(
                    "result",
                    "Result",
                    new VisualPortReference(node.Id, new PortId("value")),
                    contract),
            ]);
    }

    private static NodeId Node(string value) => NodeId.Parse(value);
}
