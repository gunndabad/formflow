using System;
using System.Collections.Generic;
using System.Linq;

namespace FormFlow;

public class JourneyDescriptor
{
    public JourneyDescriptor(
        string journeyName,
        Type stateType,
        IEnumerable<string> requestDataKeys,
        bool appendUniqueKey)
    {
        JourneyName = journeyName ?? throw new ArgumentNullException(nameof(journeyName));
        StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
        RequestDataKeys = requestDataKeys?.ToArray() ?? Array.Empty<string>();
        AppendUniqueKey = appendUniqueKey;
    }

    public bool AppendUniqueKey { get; }

    public string JourneyName { get; }

    public IReadOnlyCollection<string> RequestDataKeys { get; }

    public Type StateType { get; }
}
