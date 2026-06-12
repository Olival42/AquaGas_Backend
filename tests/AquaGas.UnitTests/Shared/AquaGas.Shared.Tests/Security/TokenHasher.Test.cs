using Xunit;
using AquaGas.Shared.Security;

namespace AquaGas.Tests.Shared.Security
{
    public class TokenHasherTests
    {
        [Fact]
        public void Hash_Should_Return_Same_Value_For_Same_Input()
        {
            var input = "meu-token";

            var hash1 = TokenHasher.Hash(input);
            var hash2 = TokenHasher.Hash(input);

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void Hash_Should_Return_Different_Value_For_Different_Input()
        {
            var input1 = "token-1";
            var input2 = "token-2";

            var hash1 = TokenHasher.Hash(input1);
            var hash2 = TokenHasher.Hash(input2);

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void Hash_Should_Not_Be_Empty()
        {
            var input = "teste";

            var hash = TokenHasher.Hash(input);

            Assert.False(string.IsNullOrWhiteSpace(hash));
        }

        [Fact]
        public void Hash_Should_Return_64_Characters()
        {
            var input = "teste";

            var hash = TokenHasher.Hash(input);

            Assert.Equal(64, hash.Length);
        }

        [Fact]
        public void Hash_Should_Work_With_Empty_String()
        {
            var input = string.Empty;

            var hash = TokenHasher.Hash(input);

            Assert.NotNull(hash);
            Assert.Equal(64, hash.Length);
        }

        [Fact]
        public void Hash_Should_Match_Known_SHA256()
        {
            var input = "abc";

            var hash = TokenHasher.Hash(input);

            Assert.Equal(
                "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD",
                hash);
        }
    }
}