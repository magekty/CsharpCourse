# C# 기초 문법 및 콘솔 입출력 학습

- 오늘 공부한 C# 기본 개념과 데이터 타입, 그리고 간단한 실습 내용을 정리합니다.

    1> 오늘 배운 주요 키워드와 문법 요약
- [namespace]: 클래스들을 논리적으로 그룹화하고 이름 충돌을 방지하는 공간입니다.
- [internal]: 같은 어셈블리(프로젝트) 내에서만 접근할 수 있도록 제한하는 접근 지정자입니다.
- [class]: 객체를 생성하기 위한 틀(설계도)이며, C# 프로그램의 기본 단위입니다.
- [static]: 객체를 생성하지 않고도 메모리에 미리 올라가 바로 호출할 수 있게 해주는 키워드입니다.
- [void]: 메서드가 실행된 후 반환(리턴)하는 값이 없음을 의미합니다.
- [string]: 텍스트 문자열 데이터를 다루기 위한 타입이며, 반드시 큰따옴표(`" "`)로 감싸야 합니다.

    2> 코드 구현 및 실습 내용
- 게임 타이틀과 버전 정보를 출력하고, 데이터 타입(정수, 실수, 불리언)을 콘솔에 표현하는 실습을 진행했습니다.

```csharp
using System;

namespace Todo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 문자열과 실수 변수 선언
            string title = "모험가 길드 ver";
            float version = 1.01f;

            int hp = 10 - 1;

            // 문자열 보간법($)을 활용한 출력
            Console.WriteLine($"{title} {version}");
            
            // 정수 출력 (1.0을 적어도 정수형으로 인식되므로 실수를 표현하려면 'f'를 붙여야 함)
            Console.WriteLine(hp);   
            
            // 실수형 데이터 표현 (뒤에 'f'를 붙여 float 타입 명시)
            Console.WriteLine(1.1f);  
            
            // 불리언(Boolean) 타입 출력
            Console.WriteLine(false); 

            // 콘솔창이 바로 꺼지지 않도록 사용자의 입력을 대기
            Console.ReadLine(); 
        }
    }
}