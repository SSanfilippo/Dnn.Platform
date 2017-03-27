﻿#region Copyright
// 
// DotNetNuke® - http://www.dnnsoftware.com
// Copyright (c) 2002-2017
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using Dnn.ExportImport.Components.Common;
using Dnn.ExportImport.Components.Controllers;
using Dnn.ExportImport.Components.Dto;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Localization;
using DotNetNuke.Web.Api;

namespace Dnn.ExportImport.Services
{
    [DnnAuthorize(StaticRoles = "Administrators")]
    public class ExportImportController : DnnApiController
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public HttpResponseMessage Export(ExportDto exportDto)
        {
            var isHostUser = UserController.Instance.GetCurrentUserInfo().IsSuperUser;
            if (!isHostUser && exportDto.PortalId != PortalSettings.PortalId)
            {
                var error = Localization.GetString("NotPortalAdmin", Constants.SharedResources);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, error);
            }

            var controller = new ExportController();
            var jobId = controller.QueueOperation(PortalSettings.UserId, exportDto);
            return Request.CreateResponse(HttpStatusCode.OK, new { jobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public HttpResponseMessage Import(ImportDto importDto)
        {
            var isHostUser = UserController.Instance.GetCurrentUserInfo().IsSuperUser;
            if (!isHostUser && importDto.PortalId != PortalSettings.PortalId)
            {
                var error = Localization.GetString("NotPortalAdmin", Constants.SharedResources);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, error);
            }

            var controller = new ImportController();
            var jobId = controller.QueueOperation(PortalSettings.UserId, importDto);
            return Request.CreateResponse(HttpStatusCode.OK, new { jobId });
        }

        [HttpGet]
        public HttpResponseMessage VerifyImportPackage(string packageId)
        {
            var controller = new ImportController();
            string message;
            var isValid = controller.VerifyImportPackage(packageId, out message);
            return isValid
                ? Request.CreateResponse(HttpStatusCode.OK)
                : Request.CreateErrorResponse(HttpStatusCode.BadRequest, message);
        }

        [HttpGet]
        public HttpResponseMessage GetImportPackages()
        {
            var controller = new ImportController();
            var packages = controller.GetImportPackages().OrderBy(package => package.Name);
            return Request.CreateResponse(HttpStatusCode.OK, packages);
        }

        // this is POST so users can't cancel using a simple browser link
        [HttpPost]
        [ValidateAntiForgeryToken]
        public HttpResponseMessage CancelProcess([FromUri] int jobId)
        {
            var controller = new BaseController();
            var cancelStatus = controller.CancelJob(PortalSettings.PortalId, jobId);
            return Request.CreateResponse(
                cancelStatus ? HttpStatusCode.OK : HttpStatusCode.BadRequest, new { success = cancelStatus });
        }

        // this is POST so users can't remove a job using a browser link
        [HttpPost]
        [ValidateAntiForgeryToken]
        public HttpResponseMessage RemoveJob([FromUri] int jobId)
        {
            var controller = new BaseController();
            var cancelStatus = controller.RemoveJob(PortalSettings.PortalId, jobId);
            return Request.CreateResponse(
                cancelStatus ? HttpStatusCode.OK : HttpStatusCode.BadRequest, new { success = cancelStatus });
        }

        [HttpGet]
        public HttpResponseMessage ProgressStatus(int jobId)
        {
            //TODO: implement
            return Request.CreateResponse(HttpStatusCode.OK, new { percentage = "10%" });
        }

        [HttpGet]
        public HttpResponseMessage LastExportUtcTime()
        {
            var controller = new BaseController();
            var lastTime = controller.GetLastExportUtcTime();
            return Request.CreateResponse(HttpStatusCode.OK, new { lastTime });
        }

        [HttpGet]
        public HttpResponseMessage AllJobs(int portalId, int? pageSize = 10, int? pageIndex = 0, int? jobType = null,
            string keywords = null)
        {
            if (!UserInfo.IsSuperUser && portalId != PortalSettings.PortalId)
            {
                var error = Localization.GetString("NotPortalAdmin", Constants.SharedResources);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, error);
            }
            var controller = new BaseController();
            var jobs = controller.GetAllJobs(portalId, pageSize, pageIndex, jobType, keywords);
            return Request.CreateResponse(HttpStatusCode.OK, jobs);
        }

        [HttpGet]
        public HttpResponseMessage JobSummary(int jobId)
        {
            var controller = new BaseController();
            var job = controller.GetJobSummary(PortalSettings.PortalId, jobId);
            return job != null
                ? Request.CreateResponse(HttpStatusCode.OK, job)
                : Request.CreateResponse(HttpStatusCode.BadRequest,
                    new { message = Localization.GetString("JobNotExist", Constants.SharedResources) });
        }

        [HttpGet]
        public HttpResponseMessage JobDetails(int jobId)
        {
            var controller = new BaseController();
            var job = controller.GetJobDetails(PortalSettings.PortalId, jobId);
            return job != null
                ? Request.CreateResponse(HttpStatusCode.OK, job)
                : Request.CreateResponse(HttpStatusCode.BadRequest,
                    new { message = Localization.GetString("JobNotExist", Constants.SharedResources) });
        }
    }
}