﻿using System;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Os.Compare.Directories;
using Musoq.Schema.Os.Directories;
using Musoq.Schema.Os.Dlls;
using Musoq.Schema.Os.Files;
using Musoq.Schema.Os.Process;
using Musoq.Schema.Os.Self;
using Musoq.Schema.Os.Zip;

namespace Musoq.Schema.Os
{
    public class OsSchema : SchemaBase
    {
        private const string DirectoriesTable = "directories";
        private const string FilesTable = "files";
        private const string DllsTable = "dlls";
        private const string ZipTable = "zip";
        private const string Self = "self";
        private const string SchemaName = "os";
        private const string ProcessesName = "processes";
        private const string DirsCompare = "dirscompare";
        private const string Single = "single";

        public OsSchema()
            : base(SchemaName, CreateLibrary())
        {
            AddSource<FilesSource>(FilesTable);
            AddTable<FilesBasedTable>(FilesTable);

            AddSource<DirectoriesSource>(DirectoriesTable);
            AddTable<DirectoriesBasedTable>(DirectoriesTable);

            AddSource<ZipSource>(ZipTable);
            AddTable<ZipBasedTable>(ZipTable);

            AddSource<ProcessesSource>(ProcessesName);
            AddTable<ProcessBasedTable>(ProcessesName);

            AddSource<OsSource>(Self);
            AddTable<OsBasedTable>(Self);

            AddSource<DllSource>(DllsTable);
            AddTable<DllBasedTable>(DllsTable);

            AddSource<CompareDirectoriesSource>(DirsCompare);
            AddTable<DirsCompareBasedTable>(DirsCompare);
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FilesTable:
                    return new FilesBasedTable();
                case DirectoriesTable:
                    return new DirectoriesBasedTable();
                case ZipTable:
                    return new ZipBasedTable();
                case ProcessesName:
                    return new ProcessBasedTable();
                case Self:
                    return new OsBasedTable();
                case DllsTable:
                    return new DllBasedTable();
                case DirsCompare:
                    return new DirsCompareBasedTable();
                case Single:
                    return new SingleRowSchemaTable();
            }

            throw new NotSupportedException($"Unsupported table {name}.");
        }

        public override RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FilesTable:
                    if (parameters[0] is IReadOnlyTable filesTable)
                        return new FilesSource(filesTable, interCommunicator);

                    return new FilesSource((string)parameters[0], (bool)parameters[1], interCommunicator);
                case DirectoriesTable:
                    if (parameters[0] is IReadOnlyTable directoriesTable)
                        return new DirectoriesSource(directoriesTable, interCommunicator);

                    return new DirectoriesSource((string)parameters[0], (bool)parameters[1], interCommunicator);
                case ZipTable:
                    return new ZipSource((string)parameters[0], interCommunicator);
                case ProcessesName:
                    return new ProcessesSource(interCommunicator);
                case Self:
                    return new OsSource();
                case DllsTable:
                    return new DllSource((string)parameters[0], (bool)parameters[1], interCommunicator);
                case DirsCompare:
                    return new CompareDirectoriesSource((string)parameters[0], (string)parameters[1], interCommunicator);
                case Single:
                    return new SingleRowSource();
            }

            throw new NotSupportedException($"Unsupported row source {name}");
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new OsLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}