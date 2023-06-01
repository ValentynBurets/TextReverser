namespace FileProcessor.Tests
{
    public class Tests
    {
        string[] reverseTypes = { "Char", "Word", "Sentence" };

        [SetUp]
        public void Setup()
        {
        }

        //[Test]
        //public void ReverseByChar_Success_SmokeTest()
        //{
        //    //arrange
        //    string testString = "test text example";
        //    int lexemCount = 0;
        //    //act
        //    string result = FileProcessorWorker.ReverseText(testString, reverseTypes[0], false, out lexemCount);
            
        //    //assert
        //    Assert.That(result, Is.EqualTo("elpmaxe txet tset"));
        //}

        //[Test]
        //public void ReverseByChar2_Success_SmokeTest()
        //{
        //    //arrange
        //    string testString = "testtextexample";
        //    int lexemCount = 0;
        //    //act
        //    string result = FileProcessorWorker.ReverseText(testString, reverseTypes[0], false, out lexemCount);

        //    //assert
        //    Assert.That(result, Is.EqualTo("elpmaxetxettset"));
        //}

        //[Test]
        //public void ReverseByWords_success_SmokeTest()
        //{
        //    //arrange
        //    string testString = "test text example";
        //    int lexemCount = 0;
        //    //act
        //    string result = FileProcessorWorker.ReverseText(testString, reverseTypes[1], false, out lexemCount);

        //    //assert
        //    Assert.That(result, Is.EqualTo("example text test"));
        //}


        //[Test]
        //public void ReverseBySenteces_Success_SmokeTest()
        //{
        //    //arrange
        //    string testString = "test text example. test.";
        //    int lexemCount = 0;
        //    //act
        //    string result = FileProcessorWorker.ReverseText(testString, reverseTypes[2], false, out lexemCount);

        //    //assert
        //    Assert.That(result, Is.EqualTo("test. test text example."));
        //}

        //[Test]
        //public void ReverseBySenteces_WithDidfferentEndSigns_Success_SmokeTest()
        //{
        //    //arrange
        //    string testString = "test text example. test. question? and ansver... To ansver me!";
        //    int lexemCount = 0;
        //    //act
        //    string result = FileProcessorWorker.ReverseText(testString, reverseTypes[2], false, out lexemCount);

        //    //assert
        //    Assert.That(result, Is.EqualTo("To ansver me! and ansver... question? test. test text example."));
        //}
    }
}