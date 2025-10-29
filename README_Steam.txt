[h2]说明[/h2]
妈的不小心传错一个文件, 把测试用的xml文件传上来了, 导致原mod一直被卡审查, 先穿一个备份上来, 等原本的审查通过我就改回去, 抱歉抱歉!
[url=https://steamcommunity.com/sharedfiles/filedetails/?id=3591517959] 原mod页面 [/url]
[url=https://steamcommunity.com/sharedfiles/filedetails/?id=3595582953] 备份mod页面 [/url]

[h2] About this mod / mod介绍 [/h2]
这个mod可以在道具上显示当前你未完成任务, 未解锁的天赋, 未建造的建筑里需要的该道具的数量, 按下shift会显示个别的任务名和数量
(注意其中有一些任务可能是测试用的, 或者是期间限定解锁的, 或者是数据里有了但是玩家还不能解锁的都会显示出来!)
支持简体中文, 繁体中文, 英语, 韩语, 日语

This mod can display the names and required quantities of unfinished quests, locked perks, and unbuilt buildings that need the current item.
(Note: Some entries may be for testing, time-limited unlocks, or exist in the data but are not yet available to players—all will be shown!)
Supports Simplified Chinese, Traditional Chinese, English, Korean, and Japanese.


[h2] To do / 待更新事项 [/h2]
现在有一些在游戏中没有开启, 但是在数据中已经存在的任务和天赋, 目前在想办法把这些都去除, 哪位老哥知道什么好方法可以告诉我一下!

There are currently some quests and perks that are not enabled in the game but still exist in the data. I’m trying to find a way to remove them all. If anyone knows a good method, please let me know! 


[h2] Bug [/h2]
目前有提交了道具, 但是任务还没有完成的话, 道具说明里还是会显示需要需要提交
这一段我是直接用的游戏里其他地方的实现代码一样的方法弄的, 但是还是会这样, 是游戏本身代码逻辑有点问题, 有空的话我会想办法优化一下
有了解的老哥告诉我一下怎么改(

After submitting the item, if the quest is not yet completed, the item description will still say it needs to be submitted.
For this part, I just used the same implementation method as in other parts of the game, but it still behaves like this. It seems to be a problem with the game's own logic. I'll try to optimize it when I have time.
If anyone knows how to fix this, please let me know!


[h2] Release Note / 更新日志 [/h2]
v0.5 - 更改了默认显示需求道具总数, 并且再按下Shift键时显示具体任务名和数量的功能
v0.4 - 现在未完成的建筑需要的道具数量也可以显示了!  
v0.3 - 现在未解锁的天赋需要的道具数量也可以显示了!  
v0.2 - 修复了道具名中有数字时不能正常显示道具数量的bug  
v0.1 - 发布  

v0.5 - Changed the default display to show the total required item count, and added a feature to show detailed quest names and quantities when holding the Shift key.
v0.4 - The required item quantities for unbuilt buildings can now be displayed!  
v0.3 - The required item quantities for perks can now be displayed!  
v0.2 - Fixed a bug where item quantities could not be displayed correctly if the item name contained numbers.  
v0.1 - Initial release.  


[url=https://github.com/ballban/QuestItemRequirementsDisplay] Github [/url]
v0.5 by ballban