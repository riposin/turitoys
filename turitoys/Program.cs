﻿string? header, option;

header = """
	Turistore toys - QA
	-------------------
	Select an option or empty to close

	A: Set materials s4h_id to POS DB based on SKU
	B: Verify material prices were updated correctly on POS DB
	""";

Console.WriteLine(header);
option = Console.ReadLine();
option = string.IsNullOrEmpty(option) ? option : option.Trim().ToLower();

switch (option)
{
	case "a":
		Program.Optiona();
		break;
	case "b":
		Program.Optionb();
		break;
	default:
		break;
}

Console.WriteLine("""

	Done, press any key to exit...
	""");
Console.ReadLine();