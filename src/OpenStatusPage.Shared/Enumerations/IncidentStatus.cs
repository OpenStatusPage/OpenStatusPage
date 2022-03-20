namespace OpenStatusPage.Shared.Enumerations;

public enum IncidentStatus
{
    Created, //For automatic creation
    Acknowledged, //Default when created manually
    Investigating,
    Monitoring,
    Resolved
}
