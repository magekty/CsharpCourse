using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace note
{
    internal class 데이터흐름제어
    {
        static void Main(string[] args)
        {
            // 12. Stack, Queue, HshSet
            /*Stack<string> history = new Stack<string>();

            history.Push("첫 작업");
            history.Push("두 번째 작업");
            Console.WriteLine(history.Pop()); // "두 번째 작업" 출력

            Queue<string> waiting = new Queue<string>();
            waiting.Enqueue("지수");
            waiting.Enqueue("현우");
            Console.WriteLine(waiting.Dequeue()); // "지수" 출력

            HashSet<string> members = new HashSet<string>();
            Console.WriteLine(members.Add("지수")); // True
            Console.WriteLine(members.Add("지수")); // False (중복이라 추가 안 됨)*/

            // 12. Test
            /*for (int i = 0; i < 3; i++)
            {
                string? name = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    waiting.Enqueue(name.Trim());
                }
            }

            while (waiting.Count > 0)
            {
                Console.WriteLine(waiting.Dequeue());
            }*/

            // 13. Error Fix Order
            /*int[] scores = { 60, 70, 80 };
            int sum = 0;
            for (int i = 0; i < scores.Length; i++)
            {
                sum += scores[i];
            }
            Console.WriteLine(sum);*/

            // 13. Test i <= To i < is Fixed
            /*int[] scores = { 60, 70, 80 };
            int sum = 0;
            for (int i = 0; i <= scores.Length; i++)
            {
                sum += scores[i];
            }
            Console.WriteLine(sum);*/

            // 14. Total Practical Training(Student Management Program)
            /*Dictionary<int, Student> students = new Dictionary<int, Student>();
            students.Add(101, new Student("지수", 80));
            students.Add(102, new Student("현우", 95));
            foreach (var entry in students)
            {
                Console.WriteLine($"{entry.Key}: {entry.Value.Name} / {entry.Value.Score}점");
            }*/

            // 14. Test
            /*Dictionary<int, Student> students = 
                new Dictionary<int, Student> { { 101, new Student("지수", 80) } };
            string idText = Console.ReadLine() ?? "";
            string name = Console.ReadLine() ?? "";
            string scoreText = Console.ReadLine() ?? "";

            if (!int.TryParse(idText, out int idCheck) ||
                !int.TryParse(scoreText, out int scoreCheck) ||
                string.IsNullOrWhiteSpace(idText) ||
                string.IsNullOrWhiteSpace(name) ||
                scoreCheck < 0 || scoreCheck > 100 )
            {
                Console.WriteLine("입력오류");
                return;
            }
            if(students.ContainsKey(idCheck))
            {
                Console.WriteLine("중복번호");
                return;
            }

            students.Add(int.Parse(idText), new Student(name, int.Parse(scoreText)));
            students.TryGetValue(idCheck, out Student? student);
            Console.WriteLine($"{student?.Name}: {student?.Score}점");*/

        }

        // 14. Practical Class
        /*class Student
        {
            public string Name { get; }
            public int Score { get; }
            public Student(string name, int score)
            {
                Name = name;
                Score = score;
            }
        }*/

        // 14. Test Class
        /*class Student
        {
            public string Name { get; }
            public int Score { get; }
            public Student(string name, int score) { Name = name; Score = score; }
        }*/
    }
}