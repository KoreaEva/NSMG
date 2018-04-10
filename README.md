# NSMG
NSMG와 Hackfest를 위해서 만든 Repository 입니다. 

## 개발환경 구성

- Visual Studio 2017
- Slack
- Git
- Github

## 사전학습자료

- Event Husbs
- Stream Analysis Job
- Azure Functions
- Cosmos DB
- Azure Storage

## Artchitecture

## 기술 검증
![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture.png?raw=true)<br>

### 1. EventHubs Client

 EventHub에 연결하기 위해서는 두 가지 방법 중 하나를 선택할 수 있다. 첫 번째 방법은 제공되는 패키지를 이용하는 방법이고 두 번째는 REST를 이용하는 연결 방법이다. 이번 Hackfest에서는 두 가지 방법을 모두 테스트 했다. 패키지를 이용하는 방법은 Nuget 을 사용하는 Console App으로 제작해서 테스트용 더미 클라이언트를 만드는데 사용했다. 
 
  REST를 사용하는 방법은 실제 Android 개발에 사용할 Library 제작에 사용 되었다. 


1초에 5000개 이상의 요청을 안정적으로 처리하기 하기 위해서 IoT Hub와 EventHub 그리고 10개의 Azure Storage Queue에 분산처리 하는 방법 등 다양한 방법이 고려되었다. 이 중에서도 초기에 EventHub는 모바일 디바이스쪽으로 Callback할 수 있는 방법이 제공되지 않아서 제외되었고 IoT Hub의 경우 비용에 대한 문제로 제외되었다. Queue를 사용하는 방식으로 거의 결졍되었다가 디바이스 방향으로의 Callback을 Google이 제공하는 push notification으로 처리하게 되면서 Event Hub를 사용하기로 결정되었다. 

EventHub는 Standard 버전의 경우 1개의 인스턴스가 초당 1000개의 메시지를 처리 할 수 있다. 따라서 10개를 사용하면 무난하게 요구사항을 맞출 수 있을 것으로 기대 된다. 
![EventHub](https://github.com/KoreaEva/NSMG/blob/master/Images/EventHubs.png?raw=true)<br>

### 3. Azure Functions (EventHub Trigger)

EventHub를 사용하게 되면서 Azure Functions도 역시 EventHub Trigger를 사용하게 되었다. 

```csharp
    public static class EventHubWifi
    {
        [FunctionName("EventHubWifi")]
        public static void Run([EventHubTrigger("wifi", Connection = "WIFI")]string myEventHubMessage, TraceWriter log)
        {
            log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");
        }
    }
```
Azure Functions를 사용할 때에 주의 할 점은 실제 가동중인 Azure Functions와 Local에서 개발중인 Azure Functions의 storage를 같은 서비스로 지정하게 되면 Connection limit가 초과 되었다는 메시지와 함께 실행되지 않는 경우가 있다. 

### 4. MySQL to SQL data migration 

### 5. SQL Utility library

### 6. Cosmos DB

Cosmos DB에 저장하기 위해서는 Database를 먼저 생성하고 JSON Document를 저장할 수 있는 Collection을 생성해야 한다. 실제 저장은 Collection에 이루어지는데 Collection도 10Gbyte 한계가 있는 버전과 Unlimited 버전이 존재한다. Unlimited을 사용할 때에는 사실상 RU의 제약이 없이 사용할 수 있으며 또 RU의 제한을 넘어서 사용하는 것도 별도의 요청을 통해서 가능하게 되어 있다. 반대로 10Gbyte의 한계가 있는 버전의 경우에는 10,000 RU가 한계다. 그래서 NSMG의 요구사항을 수용하기 위해서는 Unlimited Collection을 사용하게 되었다. 

```csharp
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;

using System;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NSMGFunctions
{
    public static class EventHubWifi
    {
        private const string EndpointUrl = "<EndPoint URL>";
        private const string PrimaryKey = "<Primary Key>";
        private static DocumentClient client = null;
        private static string DatabaseName = "<Database Name>";
        private static string DataCollectionName = "<Collection Name>";

        [FunctionName("EventHubWifi")]
        public async static void Run([EventHubTrigger("wifi", Connection = "WIFI")]string myEventHubMessage, TraceWriter log)
        {
            try
            {
                client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);

                // Azure Function의 효율을 위해서 Database와 Collection을 체크하고 생성하는 코드는 모두 주석처리 했다. 

                //await client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseName });
                //await client.CreateDocumentCollectionIfNotExistsAsync(
                //    UriFactory.CreateDatabaseUri(DatabaseName), 
                //    new DocumentCollection { Id = DataCollectionName },
                //    new RequestOptions { OfferThroughput = 5000}
                //);

                string documentID = generateID();
                JObject json = JObject.Parse(myEventHubMessage);

                log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");
                await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, DataCollectionName), json);
                
            }
            catch(DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                string errorMessage = string.Format("error occurred : {0}. Message: {1},", de.StatusCode, de.Message);
                log.Info(errorMessage);
            }
            catch(Exception e)
            {
                string errorMessage = string.Format("Message: {0},", e.Message);
                log.Info(errorMessage);
            }
            finally
            {

            }
        }

        /// 문서의 고유이름 생성하기 위한 메소드
        public static string generateID()
        {
            return string.Format("{0}_{1:N}", System.DateTime.Now.Ticks, Guid.NewGuid());
        }
    }
}
```

Cosmos DB에 필요한 RU를 계산해 볼 수 있는 사이트 [https://www.documentdb.com/capacityplanner#](https://www.documentdb.com/capacityplanner#)<br

Cosmos DB개발 참조링크
[https://docs.microsoft.com/ko-kr/azure/cosmos-db/sql-api-get-started](https://docs.microsoft.com/ko-kr/azure/cosmos-db/sql-api-get-started)<br>

### 7. Azure Batch

### 8. Azure Search

### 9. Azure Functions(Http Trigger)


## 프로젝트 코드

## Collaboration Tool

[NSMG Hackfest Slack](http://nsmg-hackfest.slack.com)<br>
[One Drive](https://1drv.ms/f/s!AosfFsO-w03gjnOhsZl1McXhzLP4)


## Azure Services

[Azure Web App](https://docs.microsoft.com/ko-kr/azure/app-service/app-service-web-overview)<br>
[IoT Hub](https://docs.microsoft.com/ko-kr/azure/iot-hub/)<br>
[Stream Analytics Job](https://docs.microsoft.com/ko-kr/azure/stream-analytics/)<br>
[Azure Storage Account](https://docs.microsoft.com/ko-kr/azure/storage/common/storage-introduction)<br>
[SQL Database](https://docs.microsoft.com/ko-kr/azure/sql-database/sql-database-technical-overview)<br>
[Azure Functions](https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview)<br>
[Cosmos DB](https://docs.microsoft.com/ko-kr/azure/cosmos-db/introduction)

## 관련 문서 


## 사용을 예상했으나 사용되지 않은 서비스 



### 1. IoT Hub Dummy --> Android Client

IoT Hub Dummy를 사용해서 대량의 데이터를 전송하는 것을 시도했으나 충분한 트래픽을 발생시키는 데에는 1개의 클라이언트에서 한계가 있었고 또 네트웍등의 문제가 발생하는 것을 확인했다. 
 또 IoT Hub가 비용적인 문제와 좀 더 유연한 확장을 위해서 Azure Storage Account에서 제공하는 Queue를 사용하게 되면서 아래의 Iot Hub Dummy는 사용하지 않게 되었다. 대신 Android Client를 만들어서 테스트하게 되었으며 Android Client는 Queue에 직접 통신하게 되었다. 

 #### Android Client
 <내용 추가>

 #### IoT Hub Dummy (사용하지 않음)

테스트르 위해서 충분한 숫자의 데이터 패킷을 발생시키기 위한 더미 소스 입니다.<br> 
[IoTHub Sensor Data Dummy C# Source Code](https://github.com/KoreaEva/NSMG/tree/master/Samples/IoTHubDataSender)<br>

사용하기 위해서는 실행 파일에서 아래와 같이 실행하게 되면 5초단위로 1개의 인스턴스에서 패킷을 발송하게 됩니다. <br>
IoTHubDataSender 5000 1<br>

충분한 숫자의 패킷을 발송하기 위해서는 IoTHubDataSender 1 5000 이렇게 실행시키면 1000/1 초 단위로 5000천개의 인스턴스에서 패킷을 발송하게 됩니다. <br>
지금은 온도, 습도, 미세먼지 값등을 더미로 발생시켜서 보내고 있다. 실제 데이터로 바꾸기 위해서는 아래 두 개의 파일을 수정해야 한다. <br>

WeatherModel.cs //데이터 모델<br>
DummySensor.cs  //난수 발생기 <br>

### 2. Azure Storage Queue

Azure Storage Queue와 관련된 자세한 내용은 [https://azure.microsoft.com/ko-kr/services/storage/queues/](https://azure.microsoft.com/ko-kr/services/storage/queues/
)에서 확인 할 수 있다. 

![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Queues.png?raw=true)<br>

여기서는 초당 5000개 이상의 요청을 처리하기 위해서 10개의 Queue를 사용 하고 있다. 그리고 각각의 Queue에는 Queue Trigger 담당하는 Azure Functions들을 연결해서 들어오는 요청을 처리 할 수 있게 했다. 

Queue의 제약은 아래와 같다. 
Azure Storage Account 초당 20,000 호출<br>
Azure Storage Queue 초당 2,000 호출<br>

### 2. IoT Hub Trigger

![IoT Hub Setting](https://github.com/KoreaEva/NSMG/blob/master/Images/IoTHubTriggerSetting.png?raw=true)
<br>

IoT Hub Trigger를 사용하기 위해서는 EndPoint에 지정된 값들을 소스에 반영해서 코딩해야 한다. 

```csharp
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;

namespace IoTHubTrigger
{
    public static class IoTHubTrigger
    {
        [FunctionName("IoTHubTrigger")]
        public static void Run([EventHubTrigger("nsmgiothub", Connection = "IoTHubConnection")]string myEventHubMessage, TraceWriter log)
        {
            log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");
        }
    }
}
```

### 3. Blob Trigger
Blob Trigger의 경우 Blob안에 저장되어 있는 파일의 Stream이 리턴된다. 따라서 업데이트 되는 부분만 추출하기 위해서는 마지막 라인만 가져오는 방법이 필요하다. 
 어떻게 해도 파일 단위의 작업이기 때문에 불가피하게 오버해드가 발생 된다. 그래서 Batch 작업등에서만 사용하는 것이 적절한 방법으로 판단된다. 
개별적인 업데이트 내용을 핸들링 하기 위해서는 IoTHub Trigger를 사용하는 것이 적합해 보인다. 

```csharp
    public static class BlobTriggerJson
    {
        [FunctionName("BlobTriggerJson")]
        public static void Run([BlobTrigger("tempdata/{name}", Connection = "BlobStorage")]Stream myBlob, string name, TraceWriter log)
        {
            StreamReader sr = new StreamReader(myBlob);

            string temp = sr.ReadLine(); //이렇게 하면 첫째 줄만 계속 읽게 되면 그래서 sr.ReadToEnd()를 사용해야 하는데 그럼 사이즈가 커지면 심각한 오버해드가 발생할 예정.

            JObject json = JObject.Parse(temp);

            log.Info($"DeviceID: {json.Last["DeviceID"].ToString()} Temperature: {json.Last["Temperature"].ToString()} Humidity: {json.Last["Humidity"].ToString()}");
            
            //log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
```

### 4. Timer Trigger

Timer setting 방법 [https://gs.saro.me/#!m=elec&jn=866](https://gs.saro.me/#!m=elec&jn=866)<br>

```csharp
    public static class TimerTrigger
    {
        [FunctionName("TimerTrigger")]
        public static void Run([TimerTrigger("0/10 * * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
```

### 6. Log Analytics

일반적인 용도로 사용할 수 없어서 제외되었다.