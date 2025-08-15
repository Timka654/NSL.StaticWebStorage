using NSL.StaticWebStorage.Client;
using NSL.StaticWebStorage.Shared.Models;

namespace NSL.StaticWebStorage.Test
{
    public class Tests
    {
        [SetUp]
        public async Task Setup()
        {
            var response = await HttpMethods.DevClearAsync(baseUrl);

            if (!response.IsSuccessStatusCode)
                throw new Exception();
            Random.Shared.NextBytes(filesContent.AsSpan());
        }

        const string baseUrl = "https://localhost:5000";

        const string masterToken = "08a516d0-f83b-4067-9827-defedfe5d6ca";
        const string masterTokenCode = "15bd8bbc-74da-4211-aba2-eaa7732103c1";

        const string sharedStorageName = "sharedStorage";
        const string noSharedStorageName = "noSharedStorage";

        [Test, TestCase(false)]
        public async Task CreateSharedStorage(bool preventPass = false)
        {
            var successResponse1 = await HttpMethods.CreateStorageAsync(masterToken, masterTokenCode, sharedStorageName, true, baseUrl);

            Assert.That(successResponse1.IsSuccessStatusCode, Is.True);

            var failedResponse1 = await HttpMethods.CreateStorageAsync(masterToken, masterTokenCode, sharedStorageName, true, baseUrl);

            Assert.That(failedResponse1.IsSuccessStatusCode, Is.False);

            var failedResponse2 = await HttpMethods.CreateStorageAsync(masterToken, masterTokenCode, sharedStorageName, false, baseUrl);

            Assert.That(failedResponse2.IsSuccessStatusCode, Is.False);

            if (!preventPass)
                Assert.Pass();
        }

        [Test, TestCase(false)]
        public async Task CreateNoSharedStorage(bool preventPass = false)
        {
            var successResponse1 = await HttpMethods.CreateStorageAsync(masterToken, masterTokenCode, noSharedStorageName, false, baseUrl);

            Assert.That(successResponse1.IsSuccessStatusCode, Is.True);

            var failedResponse1 = await HttpMethods.CreateStorageAsync(masterToken, masterTokenCode, noSharedStorageName, true, baseUrl);

            Assert.That(failedResponse1.IsSuccessStatusCode, Is.False);

            var failedResponse2 = await HttpMethods.CreateStorageAsync(masterToken, masterTokenCode, noSharedStorageName, false, baseUrl);

            Assert.That(failedResponse2.IsSuccessStatusCode, Is.False);

            if (!preventPass)
                Assert.Pass();
        }

        const string GlobalWithoutShareToken = "GlobalWithoutShareToken";
        const string GlobalWithoutDownloadToken = "GlobalWithoutDownloadToken";
        const string GlobalWithoutUploadToken = "GlobalWithoutUploadToken";
        const string GlobalTokenCode = "GlobalTokenCode";


        byte[] filesContent = new byte[1024 * 1024 * 1]; // 10 MB

        [Test, TestCase(true), TestCase(false)]
        public async Task CheckGlobalShareAccess(bool isShared)
        {
            if (isShared)
                await CreateSharedStorage(true);
            else
                await CreateNoSharedStorage(true);

            var shareResponse1 = await HttpMethods.ShareAccessAsync(masterToken, masterTokenCode, default, default, new CreateStorageTokenRequestModel()
            {
                Token = GlobalWithoutShareToken,
                Code = GlobalTokenCode,
                CanShareAccess = false,
                CanDownload = true,
                CanUpload = true
            }, baseUrl);

            Assert.That(shareResponse1.IsSuccessStatusCode, Is.True);

            await checkOperationsAccess(GlobalWithoutShareToken, GlobalTokenCode, false, true, true, true, false, isShared ? sharedStorageName : noSharedStorageName, 1);

            //


            var shareResponse2 = await HttpMethods.ShareAccessAsync(masterToken, masterTokenCode, default, default, new CreateStorageTokenRequestModel()
            {
                Token = GlobalWithoutUploadToken,
                Code = GlobalTokenCode,
                CanShareAccess = true,
                CanDownload = true,
                CanUpload = false
            }, baseUrl);

            Assert.That(shareResponse2.IsSuccessStatusCode, Is.True);

            await checkOperationsAccess(GlobalWithoutUploadToken, GlobalTokenCode, true, false, false, true, false, isShared ? sharedStorageName : noSharedStorageName, 2);

            //


            var shareResponse3 = await HttpMethods.ShareAccessAsync(masterToken, masterTokenCode, default, default, new CreateStorageTokenRequestModel()
            {
                Token = GlobalWithoutDownloadToken,
                Code = GlobalTokenCode,
                CanShareAccess = true,
                CanDownload = false,
                CanUpload = true
            }, baseUrl);

            Assert.That(shareResponse3.IsSuccessStatusCode, Is.True);

            await checkOperationsAccess(GlobalWithoutDownloadToken, GlobalTokenCode, true, true, isShared, true, false, isShared ? sharedStorageName : noSharedStorageName, 3);

            //


            Assert.Pass();
        }


        async Task checkOperationsAccess(string token
            , string code
            , bool canShare
            , bool canUpload
            , bool canDownload
            , bool globalStorage
            , bool path
            , string storage
            , int i)
        {

            string _p = $"temp/path{i}.file";

            var shareAccessResponse = await HttpMethods.ShareAccessAsync(token, code, globalStorage ? default : storage, path ? _p : default, new CreateStorageTokenRequestModel()
            {
                Token = $"errorGlobalToken{i}",
                Code = "errorGlobalToken1Code",
                CanShareAccess = true,
                CanDownload = true,
                CanUpload = true
            }, baseUrl);

            Assert.That(shareAccessResponse.IsSuccessStatusCode, canShare ? Is.True : Is.False);


            var uploadResponse = await HttpMethods.UploadAsync(token, code, storage, _p, new MemoryStream(filesContent), baseUrl);

            Assert.That(uploadResponse.IsSuccessStatusCode, canUpload ? Is.True : Is.False);


            var downloadResponse = await HttpMethods.DownloadAsync(token, code, storage, _p, baseUrl);

            Assert.That(downloadResponse.IsSuccessStatusCode, canDownload ? Is.True : Is.False);
        }

        [Test]
        public async Task CheckSharedAccess()
        {
            var successResponse1 = await HttpMethods.CreateStorageAsync(masterToken, masterTokenCode, sharedStorageName, false, baseUrl);

            Assert.That(successResponse1.IsSuccessStatusCode, Is.True);

            var successResponse2 = await HttpMethods.ShareAccessAsync(masterToken, masterTokenCode, sharedStorageName, default, new CreateStorageTokenRequestModel()
            {
                CanShareAccess = false,
                CanDownload = true,
                CanUpload = true
            }, baseUrl);

            Assert.Pass();
        }
    }
}
