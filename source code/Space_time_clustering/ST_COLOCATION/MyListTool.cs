using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Space_time_clustering.ST_COLOCATION
{
    /// <summary>
    /// A toolkit class for operation OD flow records.
    /// </summary>
    public class MyListTool
    {
        /**
        * 向ArrayList中添加元素，如果集合中已经包含不添加，不包含则添加
        * @param list 原集合
        * @param ele 需要添加的元素
        * @return 添加后的集合（如果集合中已经包含不添加）
        */
        public static List<int> listAddEle(List<int> list, int ele)
        {
            if (!list.Contains(ele))
            {
                list.Add(ele);
            }
            return list;
        }

        public static List<Record> RecordListAddEle2(List<Record> list, Record ele)
        {
            list.Add(ele);
            return list;
        }

        public static List<Record> RecordListAddEle(List<Record> list, Record ele)
        {
            bool flag = true;
            for (int i = 0; i < list.Count; i++)
            {
                //			System.out.println("\nlist.get(i):   "+list.get(i));
                //			System.out.println("ele:           "+ele);
                //			System.out.println("list.get(i).equalTo(ele):   "+list.get(i).equalTo(ele)+"\n");
                if (list[i].Equals(ele))
                {
                    flag = false;
                    break;
                }
            }
            if (flag == true)
            {
                list.Add(ele);
            }
            else
            {
                //			System.out.println(ele);
            }
            return list;
        }

        public static List<Record> RecordListAddList(List<Record> mainList, List<Record> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                MyListTool.RecordListAddEle(mainList, list[i]);
            }
            return mainList;
        }


        public static List<String> listAddEle(List<String> list, String ele)
        {
            if (!list.Contains(ele))
            {
                list.Add(ele);
            }
            return list;
        }
        /**
         * 向mainList集合中加入一个新集合，但是mainList已经包含的元素不再重复添加，如果新集合为空，返回原集合
         * @param mainList 原集合
         * @param addList 需要添加到原集合的list
         * @return 添加后的集合
         */
        public static List<int> listAddList(List<int> mainList, List<int> addList)
        {
            if (addList.Count == 0)
            {
                return mainList;
            }
            for (int i = 0; i < addList.Count; i++)
            {
                if (!mainList.Contains(addList[i]))
                {
                    mainList.Add(addList[i]);
                }
            }

            return mainList;
        }
        /**
         * 向mainList集合中加入一个新集合，但是mainList已经包含的元素不再重复添加，如果新集合为空，返回原集合
         * @param mainList 原集合
         * @param addList 需要添加到原集合的list
         * @return 添加后的集合
         */
        public static List<String> listAddStringList(List<String> mainList, List<String> addList)
        {
            if (addList.Count == 0)
            {
                return mainList;
            }
            for (int i = 0; i < addList.Count; i++)
            {
                if (!mainList.Contains(addList[i]))
                {
                    mainList.Add(addList[i]);
                }
            }

            return mainList;
        }




        /**
         * 向mainList集合中加入一个新集合，但是mainList已经包含的元素不再重复添加，如果新集合为空，返回原集合
         * @param mainList 原集合
         * @param addList 需要添加到原集合的list
         * @return 添加后的集合
         */
        public static List<List<int>> bigListAddList(List<List<int>> mainList, List<int> addList)
        {
            bool flag = true;
            for (int i = 0; i < mainList.Count; i++)
            {
                if (equalList(mainList[i], addList))
                {
                    flag = false;
                    break;
                }
            }
            if (flag == true)
            {
                mainList.Add(addList);
            }

            return mainList;
        }
        /**
         * 向mainList集合中加入一个新集合，但是mainList已经包含的元素不再重复添加，如果新集合为空，返回原集合
         * @param mainList 原集合
         * @param addList 需要添加到原集合的list
         * @return 添加后的集合
         */
        public static List<List<List<string>>> bigListStringAddList(List<List<List<string>>> mainList,
                List<List<string>> addList)
        {
            bool flag = true;
            //判断加入记录是否被原集合包含
            for (int i = 0; i < mainList.Count; i++)
            {

                if (isListContainList(mainList[i][0], addList[i]) && isListContainList(mainList[i][1], addList[1]))
                {
                    //Console.WriteLine("因为被原集合包含删除：" + addList);
                   // Console.WriteLine("原集合：" + mainList[i][0] + "," + mainList[i][1]);
                    flag = false;
                    break;
                }
            }
            if (flag == true)
            {
                //判断加入记录是否包含原集合
                for (int i = 0; i < mainList.Count; i++)
                {
                    if (isListContainList(addList[0], mainList[0][0]) && isListContainList(addList[1], mainList[i][1]))
                    {
                        //Console.WriteLine("因为包含原集合，删除原集合记录：" + mainList[i]);
                        mainList.RemoveAt(i);
                        i--;
                    }
                }
            }



            if (flag == true)
            {
                mainList.Add(addList);
            }
            else
            {
                //Console.WriteLine("因为被原集合包含删除：" + addList);
            }

            return mainList;
        }

        public static bool equalList(List<int> list1, List<int> list2)
        {
            if (list1.Count != list2.Count)
            {

                return false;
            }
            if (isListContainList(list2, list1))
            {

                return true;
            }

            return false;
        }
        public static bool equalStringList(List<String> list1, List<String> list2)
        {
            if (list1.Count != list2.Count)
            {
                //        	System.out.println("false:  "+list1+"   :  " +list2);
                return false;
            }
            if (isListContainList(list2, list1))
            {
                //        	System.out.println("true:  "+list1+"   :  " +list2);
                return true;
            }
            //        System.out.println("false:  "+list1+"   :  " +list2);
            return false;
        }

        /// <summary>
        /// 判断list1中是否完全包含了list2 string
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static bool isListContainList(List<string> list1, List<string> list2)
        {
            if (list2.All(t => list1.Any(b => b == t)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断list1中是否完全包含了list2 int
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static bool isListContainList(List<int> list1, List<int> list2)
        {
            if (list2.All(t => list1.Any(b => b == t)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
