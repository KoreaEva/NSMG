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