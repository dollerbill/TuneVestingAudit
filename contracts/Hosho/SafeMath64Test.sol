pragma solidity ^0.4.24;

import "../SafeMath64.sol";

contract SafeMathTest64 {
    using SafeMath64 for uint64;

    function mul64Overflow() public view returns (uint64) {
        uint64 max = 2**64 - 1;
        return max.mul(max);
    }

    function div64Zero() public view returns (uint64) {
        uint64 a = 0;
        return a.div(a);
    }

    function sub64Overflow() public view returns (uint64) {
        uint64 a = 5;
        return a.sub(10);
    }

    function add64Overflow() public view returns (uint64) {
        uint64 max = 2**64 - 1;
        return max.add(max);
    }

    function mod64Zero() public view returns (uint64) {
        uint64 a = 10;
        uint64 b = 0;
        return a.mod(b);
    }
}