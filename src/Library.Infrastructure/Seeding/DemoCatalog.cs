namespace Library.Infrastructure.Seeding;

public static class DemoCatalog
{
    public const int EbookLicenses = 2;

    public static readonly IReadOnlyList<DemoBook> Books =
    [
        new("Pride and Prejudice", "Jane Austen", "Romance", 1813, "9780141439518", 1342, 3,
            "Elizabeth Bennet and Mr. Darcy misjudge each other in a sharp comedy about manners, money and marriage."),
        new("Frankenstein", "Mary Shelley", "Gothic", 1818, "9780141439471", 84, 2,
            "A young scientist gives life to a creature and spends the rest of his days running from the consequences."),
        new("Dracula", "Bram Stoker", "Gothic", 1897, "9780141439846", 345, 2,
            "Letters and diaries follow a small group of friends as they hunt the Transylvanian count across England."),
        new("Moby-Dick", "Herman Melville", "Adventure", 1851, "9780142437247", 2701, 1,
            "Captain Ahab drives the crew of the Pequod across the oceans in pursuit of the white whale."),
        new("Alice's Adventures in Wonderland", "Lewis Carroll", "Fantasy", 1865, "9780141439761", 11, 2,
            "Alice follows a white rabbit into a world where logic is upside down and nothing is what it seems."),
        new("The Adventures of Sherlock Holmes", "Arthur Conan Doyle", "Mystery", 1892, "9780140437713", 1661, 3,
            "Twelve cases solved by the famous detective of Baker Street and told by his friend Dr. Watson."),
        new("Jane Eyre", "Charlotte Brontë", "Romance", 1847, "9780141441146", 1260, 2,
            "An orphan becomes a governess at Thornfield Hall and refuses to give up her independence for love."),
        new("Crime and Punishment", "Fyodor Dostoevsky", "Literary Fiction", 1866, "9780140449136", 2554, 1,
            "A poor student commits a murder in St. Petersburg and is consumed by guilt and fear."),
        new("Don Quixote", "Miguel de Cervantes", "Adventure", 1605, "9780060934347", 996, 1,
            "An old gentleman reads too many novels of chivalry and sets out to become a knight."),
        new("The Picture of Dorian Gray", "Oscar Wilde", "Gothic", 1890, "9780141439570", 174, 2,
            "A portrait ages in place of its owner while he lives a life of pleasure without consequences."),
        new("Wuthering Heights", "Emily Brontë", "Romance", 1847, "9780141439556", 768, 1,
            "A stormy love between Catherine and Heathcliff marks two families on the Yorkshire moors."),
        new("The Count of Monte Cristo", "Alexandre Dumas", "Adventure", 1844, "9780140449266", 1184, 2,
            "Wrongly imprisoned for years, Edmond Dantès escapes, finds a treasure and plans his revenge."),
        new("Dom Casmurro", "Machado de Assis", "Literary Fiction", 1899, "9780195103090", 55752, 2,
            "Bento Santiago tells his own story and wonders whether Capitu, the love of his life, betrayed him."),
        new("A Tale of Two Cities", "Charles Dickens", "Historical", 1859, "9780141439600", 98, 1,
            "London and Paris before and during the French Revolution, seen through lives that cross unexpectedly."),
        new("The Metamorphosis", "Franz Kafka", "Literary Fiction", 1915, "9780553213690", 5200, 2,
            "Gregor Samsa wakes up one morning transformed into a giant insect, and his family must deal with it."),
        new("The Odyssey", "Homer", "Epic", -700, "9780140268867", 1727, 1,
            "After the Trojan War, Odysseus spends ten years trying to return home to Ithaca."),
        new("Treasure Island", "Robert Louis Stevenson", "Adventure", 1883, "9780141321004", 120, 2,
            "Young Jim Hawkins sails with pirates in search of the buried gold of Captain Flint."),
        new("Little Women", "Louisa May Alcott", "Literary Fiction", 1868, "9780147514011", 514, 2,
            "The four March sisters grow up in New England during the American Civil War.")
    ];

    public static string CoverUrl(string isbn) => $"https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg";
}
