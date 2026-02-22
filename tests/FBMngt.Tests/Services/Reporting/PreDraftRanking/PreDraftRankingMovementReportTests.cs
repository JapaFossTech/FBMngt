using FBMngt;
using FBMngt.Data;
using FBMngt.IO;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting;
using FBMngt.Services.Reporting.FanPros;
using FBMngt.Services.Reporting.PreDraft;
using FBMngt.Services.Reporting.PreDraftRanking;
using FBMngt.Tests.TestDoubles;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMngt.Tests.Services.Reporting
                               .PreDraftRanking;

[TestFixture]
public class PreDraftRankingMovementReportTests
{
    private ConfigSettings configSettings;
    //private Mock<IPlayerRepository> _playerRepositoryMock;
    //private Mock<IPreDraftAdjustRepository> _preDraftAdjustRepoMock = null!;
    //private PlayerOffsetService _service = null!;
    //private Mock<FanProsCoreFieldsReport> _fanProsReportMock;
    //private Mock<PlayerResolver> _playerResolverMock;

    [SetUp]
    public void Setup()
    {
        configSettings = new ConfigSettings(
                            new FakeAppSettings());
        //_playerRepositoryMock = new Mock<IPlayerRepository>();
        //_preDraftAdjustRepoMock = new
        //                    Mock<IPreDraftAdjustRepository>();
        //_playerResolverMock = new Mock<PlayerResolver>(
        //                _playerRepositoryMock.Object);
        //var _indexedFileSelector = new IndexedFileSelector(0);
        //_fanProsReportMock = new Mock<FanProsCoreFieldsReport>(
        //                configSettings,
        //                _playerResolverMock.Object,
        //                new FanProsCsvReader(),
        //                _preDraftAdjustRepoMock.Object,
        //                _indexedFileSelector);
        //_service = new PlayerOffsetService(
        //                configSettings,
        //                _playerRepositoryMock.Object,
        //                _preDraftAdjustRepoMock.Object,
        //                _fanProsReportMock.Object);
    }
}
