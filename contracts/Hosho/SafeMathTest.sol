pragma solidity ^0.4.24;

import "../SafeMath.sol";

contract SafeMathTest {
    using SafeMath for uint256;

    function mulZero() public view returns (uint256) {
        uint256 a = 0;
        return a.mul(10);
    }

    function mulOverflow() public view returns (uint256) {
        uint256 max = 2**256 - 1;
        return max.mul(max);
    }

    function mul() public view returns (uint256) {
        uint256 a = 5;
        return a.mul(a);
    }

    function divZero() public view returns (uint256) {
        uint256 a = 0;
        return a.div(a);
    }

    function div() public view returns (uint256) {
        uint256 a = 10;
        return a.div(2);
    }

    function subOverflow() public view returns (uint256) {
        uint256 a = 5;
        return a.sub(10);
    }

    function sub() public view returns (uint256) {
        uint256 a = 10;
        return a.sub(5);
    }

    function addOverflow() public view returns (uint256) {
        uint256 max = 2**256 - 1;
        return max.add(max);
    }

    function add() public view returns (uint256) {
        uint256 a = 5;
        return a.add(a);
    }

    function modPass() public view returns (uint256) {
        uint256 a = 10;
        uint256 b = 5;
        return a.mod(b);
    }

    function modZero() public view returns (uint256) {
        uint256 a = 10;
        uint256 b = 0;
        return a.mod(b);
    }
}