Player hero = new Player("용사");
Player slime = new Player("슬라임", 30);

// 체력, 이름 공격했으면 어떤 식으로 데미지 log 찍어라.
// cw tab => console.writeline

hero.TakeDamage(25);
hero.PrintStatus();
slime.PrintStatus();


class Player
{
    public string Name = "";
    public int Hp = 100;

    public Player(string Name, int Hp = 100)
    {
        this.Name = Name;
        this.Hp = Hp;
    }

    public void TakeDamage(int HP) { 
        this.Hp -= HP;
    }

    public void PrintStatus() {
        Console.WriteLine($"{this.Name}: HP {this.Hp}");
    }
}