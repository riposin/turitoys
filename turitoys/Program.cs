string? header, option;

header = """
    Turistore toys - QA
    -------------------
    Select an option or empty to close

    A: Set materials s4hid from csv to db pos based on sku
    """;

Console.WriteLine(header);
option = Console.ReadLine();
option = string.IsNullOrEmpty(option) ? option : option.Trim().ToLower();

switch (option)
{
	case "a":
		Program.Optiona();
		break;
	default:
		break;
}

Console.WriteLine("""

    Done, press any key to exit...
    """);
Console.ReadLine();