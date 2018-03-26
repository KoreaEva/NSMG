# NSMG
NSMG와 Hackfest를 위해서 만든 Repository 입니다. 

## 개발환경 구성

- Visual Studio 2017
- Slack
- Git
- Github

## 사전학습자료

- IoT Hub
- Stream Analysis Job
- Azure Functions
- Cosmos DB
- Azure Storage

## Artchitecture

## 기술 검증

## 프로젝트 코드

테스트르 위해서 충분한 숫자의 데이터 패킷을 발생시키기 위한 더미 소스 입니다. 
[IoTHub Sensor Data Dummy C# Code](https://github.com/KoreaEva/NSMG/tree/master/Samples/IoTHubDataSender)<br>

사용하기 위해서는 실행 파일에서 아래와 같이 실행하게 되면 5초단위로 1개의 인스턴스에서 패킷을 발송하게 됩니다. 
IoTHubDataSender 5000 1

충분한 숫자의 패킷을 발송하기 위해서는 IoTHubDataSender 1 5000 이렇게 실행시키면 1000/1 초 단위로 5000천개의 인스턴스에서 패킷을 발송하게 됩니다. 
지금은 온도, 습도, 미세먼지 값등을 더미로 발생시켜서 보내고 있다. 실제 데이터로 바꾸기 위해서는 아래 두 개의 파일을 수정해야 한다. 

WeatherModel.cs //데이터 모델
DummySensor.cs  //난수 발생기 


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


