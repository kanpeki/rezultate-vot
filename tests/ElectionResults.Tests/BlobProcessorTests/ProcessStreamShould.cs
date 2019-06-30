﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Models;
using ElectionResults.Core.Services.CsvProcessing;
using ElectionResults.Core.Storage;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ElectionResults.Tests.BlobProcessorTests
{
    public class ProcessStreamShould
    {
        private readonly IDataAggregator _dataAggregator;
        private readonly IElectionConfigurationSource _electionConfigurationSource;
        private readonly IResultsRepository _resultsRepository;
        private readonly string _fileName;

        public ProcessStreamShould()
        {
            _dataAggregator = Substitute.For<IDataAggregator>();
            _electionConfigurationSource = Substitute.For<IElectionConfigurationSource>();
            _resultsRepository = Substitute.For<IResultsRepository>();
            _fileName = "a_b_1";
        }

        [Fact]
        public async Task convert_stream_to_string()
        {
            var blobProcessor = CreateTestableBlobProcessor();
            MapDataAggregatorToSuccessfulResult();

            await blobProcessor.ProcessStream(new MemoryStream(), _fileName);

            blobProcessor.CsvWasReadAsString.Should().BeTrue();
        }

        [Fact]
        public async Task apply_data_aggregators_to_the_csv_content()
        {
            var blobProcessor = CreateTestableBlobProcessor();
            MapDataAggregatorToSuccessfulResult();

            await blobProcessor.ProcessStream(new MemoryStream(), _fileName);

            await _dataAggregator.ReceivedWithAnyArgs(1).RetrieveElectionData("");
        }

        [Fact]
        public async Task apply_at_least_one_aggregator()
        {
            var blobProcessor = CreateTestableBlobProcessor();
            MapDataAggregatorToSuccessfulResult();

            await blobProcessor.ProcessStream(new MemoryStream(), _fileName);

            _dataAggregator.CsvParsers.Should().NotBeEmpty();
        }

        [Fact]
        public async Task save_json_in_database()
        {
            var blobProcessor = CreateTestableBlobProcessor();
            MapDataAggregatorToSuccessfulResult();

            await blobProcessor.ProcessStream(new MemoryStream(), _fileName);

            await _resultsRepository.ReceivedWithAnyArgs(1).InsertResults(null);
        }

        [Fact]
        public async Task initialize_candidates_from_config()
        {
            var blobProcessor = CreateTestableBlobProcessor();
            MapDataAggregatorToSuccessfulResult();
            MapConfigurationSourceToEmptyListOfCandidates();

            await blobProcessor.ProcessStream(new MemoryStream(), _fileName);

            await _electionConfigurationSource.ReceivedWithAnyArgs(1).GetListOfCandidates();
        }

        private void MapConfigurationSourceToEmptyListOfCandidates()
        {
            var candidatesList = Task.FromResult(new List<Candidate>());
            _electionConfigurationSource.GetListOfCandidates().ReturnsForAnyArgs(candidatesList);
        }

        private TestableBlobProcessor CreateTestableBlobProcessor()
        {
            return new TestableBlobProcessor(_resultsRepository, _electionConfigurationSource, _dataAggregator);
        }

        private void MapDataAggregatorToSuccessfulResult()
        {
            _dataAggregator.RetrieveElectionData("")
                .ReturnsForAnyArgs(Task.FromResult(Result.Ok(new ElectionResultsData())));
        }
    }
}
