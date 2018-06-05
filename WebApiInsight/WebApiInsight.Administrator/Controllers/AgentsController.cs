﻿using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Web.Mvc;
using WebApiInsight.Administrator.Models;

namespace WebApiInsight.Administrator.Controllers
{
    [Authorize]
    public class AgentsController : Controller
    {
        const string AgentPingResponse = "it's web monitor agent";

        public ActionResult Index()
        {
            var agentManager = new AgentsManager();
            var agents = agentManager.GetAgents();
            return View(agents);
        }

        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Add(AddAgentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Неправильные данные формы создания.");
                return View();
            }
            var agentManager = new AgentsManager();
            var existedAgent = agentManager
                .GetAgents()
                .FirstOrDefault(a => a.IpAddress == model.IpAddress && a.Port == model.Port);
            if (existedAgent != null)
            {
                ModelState.AddModelError("", "Агент уже зарегистрирован.");
                return View();
            }
            var agentAddress = string.Format("http://{0}:{1}", model.IpAddress, model.Port);
            var agentResponse = PingAgent(agentAddress);
            if (agentResponse.Equals(AgentPingResponse))
            {
                //var agentSettings = GetAgentConfig(agentAddress);
                var agent = new AgentSettings
                {
                    Status = AgentStatus.Working,
                    CreationDate = DateTime.UtcNow,
                    IpAddress = model.IpAddress,
                    Port = model.Port
                };
                agentManager.Add(agent);
                return RedirectToAction("Index", "Agents");
            }
            ModelState.AddModelError("", "Не удалось установить соединение с агентом.");
            return View();
        }

        public ActionResult Settings(int id)
        {
            var agentManager = new AgentsManager();
            var agent = agentManager.FindById(id);
            if(agent == null)
            {
                ModelState.AddModelError("", string.Format("Агент с Id={0} не найден.", id));
                return View();
            }

            var config = GetAgentConfig(string.Format("http://{0}:{1}/", agent.IpAddress, agent.Port));
            var json = JsonConvert.SerializeObject(config, Formatting.Indented); ;//new JavaScriptSerializer().Serialize(config, Formatting.Indented);
            agent.JsonConfig = json;
            return View(agent);

        }

        private void Test(string agentBaseAddress)
        {
            //var SecurityToken = string.Empty;
            //using (var client = new HttpClient { BaseAddress = new Uri(agentBaseAddress) })
            //{

            //    if (!string.IsNullOrEmpty(SecurityToken))
            //        client.DefaultRequestHeaders.Add("Authorization", SecurityToken);

            //    var relativeUrl = "api/Configuration";


            //    var result = client.PostAsJsonAsync(relativeUrl, requestDto).Result;

            //    EnsureSuccess(result);

            //    return result.Content.ReadAsAsync<ApiResponse>().Result;

            //}
        }

        public string PingAgent(string agentBaseAddress)
        {
            var SecurityToken = string.Empty;//todo: user token for the api requests
            using (var client = new HttpClient { BaseAddress = new Uri(agentBaseAddress) })
            {
                if (!string.IsNullOrEmpty(SecurityToken))
                    client.DefaultRequestHeaders.Add("Authorization", SecurityToken);
                var relativeUrl = "api/Ping";
                try
                {
                    var response = client.GetAsync(relativeUrl).Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        return string.Empty;
                    }
                    var result = response.Content.ReadAsAsync<string>().Result;
                    return result;
                }
                catch(Exception ex)
                {
                    return string.Empty;
                }
            }
        }
        
        public MetricsConfigContainer GetAgentConfig(string agentBaseAddress)
        {
            var SecurityToken = string.Empty;//todo: user token for the api requests
            using (var client = new HttpClient { BaseAddress = new Uri(agentBaseAddress) })
            {
                if (!string.IsNullOrEmpty(SecurityToken))
                    client.DefaultRequestHeaders.Add("Authorization", SecurityToken);
                var relativeUrl = "api/Configuration";
                var response = client.GetAsync(relativeUrl).Result;
                EnsureSuccess(response);
                var result = response.Content.ReadAsAsync<MetricsConfigContainer>().Result;
                return result;
            }
        }
        
        private static void EnsureSuccess(HttpResponseMessage result)
        {
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception(string.Format("Ошибка вызова интерфейса конфигурации агента."));
            }
        }
    }

    public class MetricsConfigContainer//todo: to shared assembly
    {
        public MetricConfigItem[] AspNetMetricsConfig { get; set; }
        public MetricConfigItem[] ProccessMetricsConfig { get; set; }
    }

    public class MetricConfigItem
    {
        public string Measurement { get; set; }
        public string CategoryName { get; set; }
        public string CounterName { get; set; }
    }
}