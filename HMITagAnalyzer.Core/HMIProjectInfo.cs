using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using log4net;
using SEL.API;
using SEL.API.Controls;

namespace HMITagAnalyzer;

using TagName = string;
using DiagramName = string;

public class HMIProjectInfo
{
    private readonly ILog _logger;

    public HMIProjectInfo(string projectPath, ILog logger)
    {
        _logger = logger;
        InvalidTags = new List<TagName>();
        TagUsages = new Dictionary<TagName, List<(DynamicControl, DiagramName)>>();

        var originalStdErr = Console.Error;
        try
        {
            // Create a StringWriter to capture stderr
            var stringWriter = new StringWriter();
            // Redirect stderr to the StringWriter
            Console.SetError(stringWriter);

            var project = new DynamicProject(projectPath);


            // Retrieve the diagram list
            // log($"Found {project.DiagramList.Count} diagrams in the project.");

            foreach (var diagramId in project.DiagramList.Keys)
            {
                var diagramTitle = project.DiagramList[diagramId];
                Log($"Processing Diagram: {diagramTitle} (ID: {diagramId})");

                var diagram = project.GetDiagramById(new Guid(diagramId));
                // var diagramTitle = diagram.DiagramTitle;
                Log($" Processing {diagram.Controls.Count} controls");
                foreach (var control in diagram.Controls)
                {
                    var tag = control.Tag;
                    if (control is SubstitutionControl substitutionControl)
                    {
                        Debug.Assert(substitutionControl != null, nameof(substitutionControl) + " != null");
                        var perDiagramTagSubs = substitutionControl.TagSubstitutionDefinition
                            .TagSubstitutionSyntheticDiagramDefinitions;
                        Debug.Assert(perDiagramTagSubs != null, nameof(perDiagramTagSubs) + " != null");
                        foreach (var tagSub in perDiagramTagSubs)
                        foreach (var tagSubPair in tagSub.TagPairs)
                        {
                            var systemTagName = tagSubPair.SystemTag.Name;
                            // Console.Error.WriteLine($"UDT on {diagramTitle}: {udtTagName}  ->  {systemTagName}");
                            RecordTag(systemTagName, substitutionControl,
                                tagSub.DiagramTitle);
                        }
                    }
                    else if (tag != null && tag.TagName != "")
                    {
                        if (tag.TagName.StartsWith("UDT_"))
                        {
                            // Every synthetic diagram template will have:
                            //  - many Controls using UDT_* tags
                            //  - one Substitution Definition Control, mapping each UDT_* tag to N system tags
                            // We have processed the SDC above. If if() statement is for when we encounter a control using UDT_* tag. We just ignore it.
                        }
                        else
                        {
                            // Non-UDT tag
                            RecordTag(tag.TagName, control, diagram.DiagramTitle);
                        }
                    }
                    // Console.Error.WriteLine($"Got null tag {tag}");
                }

                Log($" Tag count: {TagUsages.Count}");
            }

            var capturedOutput = stringWriter.ToString();
            var unusedTags = capturedOutput.Split(["\n"], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Replace("No tag was found with the name: ", ""));
            foreach (var tag in unusedTags) InvalidTags.Add(tag);
        }
        finally

        {
            // Restore the original standard error stream
            Console.SetError(originalStdErr);
        }
    }

    public List<TagName> InvalidTags { get; }
    public Dictionary<TagName, List<ValueTuple<DynamicControl, DiagramName>>> TagUsages { get; }

    private void Log(string s)
    {
        _logger.Info(s);
    }

    public Dictionary<TagName, Dictionary<DynamicControl, List<DiagramName>>> ReusedTagLocations()
    {
        return TagUsages.ToDictionary(
                kvp => kvp.Key, // Preserve the TagName as the outer dictionary key
                kvp => kvp.Value
                    .GroupBy(tuple => tuple.Item1) // Group by DynamicControl (Item1 of the tuple)
                    .ToDictionary(
                        g => g.Key, // Use DynamicControl as the inner dictionary key
                        g => g.Select(tuple => tuple.Item2)
                            .ToList() // Collect DiagramName (Item2 of the tuple) into a list
                    )
            ).Where(kv =>
                kv.Value.Keys.Count(control => !(control is SubstitutionControl)) > 1)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private void RecordTag(string tag,
        DynamicControl control, string diagramTitle)
    {
        if (!TagUsages.ContainsKey(tag)) TagUsages[tag] = new List<(DynamicControl, DiagramName)>();
        TagUsages[tag].Add(ValueTuple.Create(control, diagramTitle));
    }
}