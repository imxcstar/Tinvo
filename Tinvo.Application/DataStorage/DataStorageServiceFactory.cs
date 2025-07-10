using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;

namespace Tinvo.Application.DataStorage
{
    public class DataStorageServiceFactory : IDataStorageServiceFactory
    {
        private readonly IDataStorageService _defaultdDataStorageService;
        private readonly ICryptographyService _cryptographyService;

        private IDataStorageService? _currentDataStorageService = null;

        public DataStorageServiceFactory(IDataStorageService defaultdDataStorageService, ICryptographyService cryptographyService)
        {
            _defaultdDataStorageService = defaultdDataStorageService;
            _cryptographyService = cryptographyService;
        }

        public async Task<IDataStorageService> CreateAsync()
        {
            if (_currentDataStorageService != null)
                return _currentDataStorageService;
            var settingInfo = await _defaultdDataStorageService.GetItemAsync<DataStorageSettingInfo>("dataStorage") ?? new DataStorageSettingInfo()
            {
                Type = DataStorageType.Local
            };
            _currentDataStorageService = settingInfo.Type switch
            {
                DataStorageType.WebDav => new WebDavStorageService(new HttpClient(), settingInfo.WebDavURI!, settingInfo.WebDavUserName!, string.IsNullOrEmpty(settingInfo.WebDavPassword) ? "" : _cryptographyService.Decrypt(settingInfo.WebDavPassword)),
                _ => _defaultdDataStorageService,
            };
            return _currentDataStorageService;
        }

        public async Task<DataStorageSettingInfo> GetConfigAsync()
        {
            var ret = await _defaultdDataStorageService.GetItemAsync<DataStorageSettingInfo>("dataStorage") ?? new DataStorageSettingInfo()
            {
                Type = DataStorageType.Local
            };
            if (ret.Type == DataStorageType.WebDav && !string.IsNullOrEmpty(ret.WebDavPassword))
                ret.WebDavPassword = _cryptographyService.Decrypt(ret.WebDavPassword);
            return ret;
        }

        public async Task SaveConfigAsync(DataStorageSettingInfo setting)
        {
            var data = setting.Adapt<DataStorageSettingInfo>();
            if (data.Type == DataStorageType.WebDav && !string.IsNullOrEmpty(data.WebDavPassword))
                data.WebDavPassword = _cryptographyService.Encrypt(data.WebDavPassword);
            await _defaultdDataStorageService.SetItemAsync<DataStorageSettingInfo>("dataStorage", data);
            _currentDataStorageService = null;
        }
    }
}
