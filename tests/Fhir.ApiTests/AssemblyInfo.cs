// Features run in parallel; every scenario creates its own uniquely identified resources.
// Kept at 3 workers to be polite to the shared public HAPI FHIR server.
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(3)]
