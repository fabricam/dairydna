namespace DairyDNA.Web.Store.Shared;

public sealed record NetworkPointDto(Guid id, string kind, string name, decimal latitude, decimal longitude);
